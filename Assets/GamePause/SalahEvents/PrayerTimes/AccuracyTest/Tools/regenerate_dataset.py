#!/usr/bin/env python3
"""
Regenerate the prayer-time accuracy reference dataset.

Downloads 4048 records from the AlAdhan API (api.aladhan.com/v1/timings):
    22 (city, method) pairs x 2 Asr schools x 92 dates in 2026 (every 4 days)

and builds Assets/GamePause/SalahEvents/PrayerTimes/AccuracyTest/Resources/prayer_times_dataset.json
with the exact schema consumed by PrayerTimeAccuracyEngine (JsonUtility POCOs).

Usage:
    python regenerate_dataset.py                     # download + build (~10 min)
    python regenerate_dataset.py --from-raw DIR      # build from cached raw JSONs
    python regenerate_dataset.py --out FILE          # custom output path

Requirements: Python 3.9+ (zoneinfo), requests.  On Windows without system
timezone data:  pip install tzdata

Endpoint per record:
    GET https://api.aladhan.com/v1/timings/{YYYY-MM-DD}
        ?latitude={lat}&longitude={lon}&method={method_id}
        &school={0=Standard(Shafi) | 1=Hanafi}
        &timezonestring={IANA timezone}

Response fields used:
    data.timings.{Fajr,Sunrise,Dhuhr,Asr,Sunset,Maghrib,Isha}  ("HH:MM" local)
    data.date.hijri.day / data.date.hijri.month.number
    data.meta.school  (validated against the requested school)

Conventions encoded here (verified against the API, see README.md):
    * utc_offset  = UTC offset at LOCAL NOON of the record date (hours),
      computed from the IANA timezone (zoneinfo). The reference engine's
      local times follow this convention, including across DST transitions.
    * ramadan     = (method == UmmAlQura and hijri_month_number == 9).
      During Ramadan UmmAlQura sets Isha = Maghrib + 120 min.
"""
import json, os, re, sys, time, threading
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import date, datetime, timedelta, timezone

try:
    from zoneinfo import ZoneInfo
except ImportError:
    sys.exit("Python 3.9+ required (zoneinfo).")

try:
    import requests
except ImportError:
    requests = None  # only needed for download mode

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_OUT = os.path.normpath(os.path.join(HERE, "..", "Resources", "prayer_times_dataset.json"))
RAW_DIR = os.path.join(HERE, "raw")

METHODS = {
    "Qom": 0, "Karachi": 1, "ISNA": 2, "MuslimWorldLeague": 3,
    "UmmAlQura": 4, "EgyptianAuthority": 5, "Tehran": 7,
    "Singapore": 11, "MuslimsOfFrance": 12, "Russia": 14,
}

# (city, country, lat, lon, IANA tz, method) - Tehran appears twice (Qom + Tehran methods)
CITIES = [
    ("Cairo",         "Egypt",         30.0444,  31.2357, "Africa/Cairo",        "EgyptianAuthority"),
    ("Jeddah",        "Saudi Arabia",  21.5433,  39.1728, "Asia/Riyadh",         "UmmAlQura"),
    ("Mecca",         "Saudi Arabia",  21.4225,  39.8262, "Asia/Riyadh",         "UmmAlQura"),
    ("London",        "UK",            51.5074,  -0.1278, "Europe/London",       "MuslimWorldLeague"),
    ("New York",      "USA",           40.7128, -74.0060, "America/New_York",    "ISNA"),
    ("Toronto",       "Canada",        43.6532, -79.3832, "America/Toronto",     "ISNA"),
    ("Sydney",        "Australia",    -33.8688, 151.2093, "Australia/Sydney",    "MuslimWorldLeague"),
    ("Auckland",      "New Zealand",  -36.8509, 174.7645, "Pacific/Auckland",    "MuslimWorldLeague"),
    ("Tokyo",         "Japan",         35.6762, 139.6503, "Asia/Tokyo",          "MuslimWorldLeague"),
    ("Kuala Lumpur",  "Malaysia",       3.1390, 101.6869, "Asia/Kuala_Lumpur",   "MuslimWorldLeague"),
    ("Istanbul",      "Turkey",        41.0082,  28.9784, "Europe/Istanbul",     "MuslimWorldLeague"),
    ("Karachi",       "Pakistan",      24.8607,  67.0011, "Asia/Karachi",        "Karachi"),
    ("Tehran",        "Iran",          35.6892,  51.3890, "Asia/Tehran",         "Tehran"),
    ("Tehran",        "Iran",          35.6892,  51.3890, "Asia/Tehran",         "Qom"),
    ("Moscow",        "Russia",        55.7558,  37.6173, "Europe/Moscow",       "Russia"),
    ("Oslo",          "Norway",        59.9139,  10.7522, "Europe/Oslo",         "MuslimWorldLeague"),
    ("Stockholm",     "Sweden",        59.3293,  18.0686, "Europe/Stockholm",    "MuslimWorldLeague"),
    ("Helsinki",      "Finland",       60.1699,  24.9384, "Europe/Helsinki",     "MuslimWorldLeague"),
    ("Reykjavik",     "Iceland",       64.1466, -21.9426, "Atlantic/Reykjavik",  "MuslimWorldLeague"),
    ("Cape Town",     "South Africa", -33.9249,  18.4241, "Africa/Johannesburg", "MuslimWorldLeague"),
    ("Singapore",     "Singapore",      1.3521, 103.8198, "Asia/Singapore",      "Singapore"),
    ("Paris",         "France",        48.8566,   2.3522, "Europe/Paris",        "MuslimsOfFrance"),
]

# AlAdhan 'school' param: 0 = Standard (Shafi, Asr = 1x shadow), 1 = Hanafi (2x shadow)
ASR_VARIANTS = [("Standard", "0"), ("Hanafi", "1")]

YEAR = 2026
STEP_DAYS = 4
N_DATES = 92
TIME_RE = re.compile(r"^\d{2}:\d{2}$")
PRAYERS = [("Fajr", "fajr"), ("Sunrise", "sunrise"), ("Dhuhr", "dhuhr"),
           ("Asr", "asr"), ("Maghrib", "maghrib"), ("Isha", "isha")]


def raw_path(raw_dir, method, asr_name, city, datestr, tz):
    key = f"{method}_{asr_name}_{city}_{datestr}_{tz.replace('/', '_')}".replace(" ", "s")
    return os.path.join(raw_dir, key + ".json")


def make_tasks():
    tasks = []
    d0 = date(YEAR, 1, 1)
    for (city, country, lat, lon, tz, method) in CITIES:
        for (asr_name, school) in ASR_VARIANTS:
            for k in range(N_DATES):
                d = (d0 + timedelta(days=STEP_DAYS * k)).isoformat()
                tasks.append({"city": city, "country": country, "lat": lat, "lon": lon,
                              "tz": tz, "method": method, "method_id": METHODS[method],
                              "asr": asr_name, "school": school, "date": d})
    return tasks


# ---------------------------------------------------------------- download ---
def download_all(tasks, raw_dir, workers=4):
    if requests is None:
        sys.exit("requests is required for download mode: pip install requests")
    os.makedirs(raw_dir, exist_ok=True)
    todo = [t for t in tasks if not os.path.exists(raw_path(raw_dir, t["method"], t["asr"], t["city"], t["date"], t["tz"]))]
    print(f"total={len(tasks)}  cached={len(tasks)-len(todo)}  to_download={len(todo)}", flush=True)
    if not todo:
        return

    sess = requests.Session()
    sess.headers.update({"User-Agent": "GamePauseAccuracyTest/1.0 (dataset regeneration)"})
    lock = threading.Lock()
    state = {"done": 0, "fail": 0}

    def fetch(t, attempt=0):
        url = f"https://api.aladhan.com/v1/timings/{t['date']}"
        params = {"latitude": t["lat"], "longitude": t["lon"], "method": t["method_id"],
                  "school": t["school"], "timezonestring": t["tz"]}
        expected = "HANAFI" if t["asr"] == "Hanafi" else "STANDARD"
        try:
            r = sess.get(url, params=params, timeout=25)
            if r.status_code == 200:
                j = r.json()
                if j.get("code") == 200 and "timings" in j.get("data", {}):
                    meta = j["data"].get("meta", {})
                    if str(meta.get("school", "")).upper() != expected:
                        raise RuntimeError(f"school mismatch: requested {expected}, got {meta.get('school')}")
                    p = raw_path(raw_dir, t["method"], t["asr"], t["city"], t["date"], t["tz"])
                    tmp = p + ".tmp"
                    with open(tmp, "w") as f:
                        json.dump(j, f)
                    os.replace(tmp, p)
                    with lock:
                        state["done"] += 1
                        if state["done"] % 100 == 0:
                            print(f"  {state['done']}/{len(todo)} downloaded", flush=True)
                    return
            if r.status_code in (429, 500, 502, 503, 504) or attempt < 5:
                time.sleep(min(60, 2 ** attempt))
                return fetch(t, attempt + 1)
        except RuntimeError:
            raise
        except Exception:
            if attempt < 5:
                time.sleep(min(60, 2 ** attempt))
                return fetch(t, attempt + 1)
        with lock:
            state["fail"] += 1
        print(f"  FAILED {t['method']} {t['asr']} {t['city']} {t['date']}", flush=True)

    with ThreadPoolExecutor(max_workers=workers) as ex:
        list(ex.map(lambda t: fetch(t), todo))
    print(f"download done: ok={state['done']} fail={state['fail']}", flush=True)
    if state["fail"]:
        sys.exit("some records failed; re-run to retry (cached records are skipped)")


# ------------------------------------------------------------------- build ---
def utc_offset_at_noon(tz, datestr):
    y, m, d = map(int, datestr.split("-"))
    dt = datetime(y, m, d, 12, 0, 0, tzinfo=ZoneInfo(tz))
    off = dt.utcoffset()
    return off.total_seconds() / 3600.0


def build(tasks, raw_dir, out_path):
    records = []
    missing = 0
    for t in sorted(tasks, key=lambda x: (x["method"], x["asr"], x["city"], x["date"])):
        p = raw_path(raw_dir, t["method"], t["asr"], t["city"], t["date"], t["tz"])
        if not os.path.exists(p):
            missing += 1
            continue
        j = json.load(open(p))
        data = j["data"]
        timings = data["timings"]
        for api_name, _ in PRAYERS:
            if not TIME_RE.match(timings[api_name]):
                sys.exit(f"bad timing format {timings[api_name]!r} in {p}")
        hijri = data["date"]["hijri"]
        rec = {
            "city": t["city"], "country": t["country"],
            "lat": t["lat"], "lon": t["lon"],
            "tz": t["tz"], "date": t["date"],
            "method": t["method"], "method_id": t["method_id"],
            "asr_school": t["asr"],
            "hijri_day": int(hijri["day"]), "hijri_month_number": int(hijri["month"]["number"]),
            "utc_offset": utc_offset_at_noon(t["tz"], t["date"]),
        }
        for api_name, key in PRAYERS:
            rec[key] = timings[api_name]
        rec["ramadan"] = (t["method"] == "UmmAlQura" and rec["hijri_month_number"] == 9)
        records.append(rec)

    if missing:
        sys.exit(f"{missing} raw records missing from {raw_dir} - run download first")

    out = {
        "source": ("AlAdhan API v1 (api.aladhan.com/v1/timings), downloaded 2026, Meezaan/PrayerTimes engine "
                   "(PrayTimes.org algorithm family). Asr school: Standard (Shafi) or Hanafi (school=1). "
                   "High-latitude rule: ANGLE_BASED (API default). "
                   "utc_offset = UTC offset at local noon of the record date (matches API's local time)."),
        "year": YEAR,
        "date_step_days": STEP_DAYS,
        "record_count": len(records),
        "prayers_compared": ["fajr", "sunrise", "dhuhr", "asr", "maghrib", "isha"],
        "time_format": "HH:MM 24h local wall clock at record location (rounded to minute with +30s)",
        "records": records,
        "note": 'ramadan flag = UmmAlQura records in Hijri month 9 (Ramaḍān); during Ramadan the reference uses Isha = Maghrib + 120 min instead of 90. Reference rounds each time by +30 s then truncates to the minute.',
    }
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, "w") as f:
        json.dump(out, f)
    print(f"wrote {out_path} ({len(records)} records, {os.path.getsize(out_path)//1024} KB)")
    ram = sum(1 for r in records if r["ramadan"])
    print(f"ramadan-flagged records: {ram}")
    return records


def main():
    args = sys.argv[1:]
    out = DEFAULT_OUT
    raw_dir = RAW_DIR
    i = 0
    while i < len(args):
        if args[i] == "--out":
            out = args[i + 1]; i += 2
        elif args[i] == "--from-raw":
            raw_dir = args[i + 1]; i += 2
        elif args[i] == "--workers":
            workers = int(args[i + 1]); i += 2
        else:
            sys.exit(f"unknown argument: {args[i]}")
    workers = locals().get("workers", 4)
    tasks = make_tasks()
    if "--from-raw" not in args:
        download_all(tasks, raw_dir, workers)
    build(tasks, raw_dir, out)


if __name__ == "__main__":
    main()
