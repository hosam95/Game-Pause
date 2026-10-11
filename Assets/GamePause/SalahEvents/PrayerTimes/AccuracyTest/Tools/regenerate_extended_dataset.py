#!/usr/bin/env python3
"""
Build the EXTENDED accuracy dataset (Resources/prayer_times_dataset_extended.json).

The baseline dataset (regenerate_dataset.py) is left untouched so its numbers stay comparable.
This one adds what the baseline lacks (see README.md, "Extended dataset"):

  1. latitude ladder  - MuslimWorldLeague at ~40, 45, 50, 55, 60, 63 degrees N (separates
                        method effect from latitude effect); Standard + Hanafi Asr, every 4 days.
  2. other high-latitude rules - the same method with AlAdhan latitudeAdjustmentMethod
                        1 (middle of night) and 2 (one-seventh of night); Standard Asr.
  3. solstice / clamp-transition sampling - DAILY dates around the northern summer clamp season
                        (1 May - 15 Aug) and the winter solstice (1 Dec - 10 Jan) for latitudes >= 50.

All records come from AlAdhan (same source and conventions as the baseline) and carry
`source` and `high_lat_rule` fields. Records from the independent JPL ephemeris are made by
regenerate_jpl_dataset.py.

Usage:
    python regenerate_extended_dataset.py --raw-dir DIR [--out FILE] [--workers N]
"""
import json, os, sys
from datetime import date, timedelta

import regenerate_dataset as base

HERE = base.HERE
DEFAULT_OUT = os.path.normpath(os.path.join(HERE, "..", "Resources", "prayer_times_dataset_extended.json"))

# (city, country, lat, lon, IANA tz) - all MuslimWorldLeague; lat ladder 40 -> 63.4 degrees N.
LADDER = [
    ("Madrid",      "Spain",    40.4168,  -3.7038, "Europe/Madrid"),
    ("Milan",       "Italy",    45.4642,   9.1900, "Europe/Rome"),
    ("Frankfurt",   "Germany",  50.1109,   8.6821, "Europe/Berlin"),
    ("Copenhagen",  "Denmark",  55.6761,  12.5683, "Europe/Copenhagen"),
    ("Bergen",      "Norway",   60.3913,   5.3221, "Europe/Oslo"),
    ("Trondheim",   "Norway",   63.4305,  10.3951, "Europe/Oslo"),
]
# Cities (all > 48 N, where the angle is unreachable for part of the year) for the non-default rules.
RULE_CITIES = [
    ("Frankfurt",   "Germany",  50.1109,   8.6821, "Europe/Berlin"),
    ("Oslo",        "Norway",   59.9139,  10.7522, "Europe/Oslo"),
    ("Bergen",      "Norway",   60.3913,   5.3221, "Europe/Oslo"),
    ("Reykjavik",   "Iceland",  64.1466, -21.9426, "Atlantic/Reykjavik"),
]
RULES = {"MiddleOfNight": "1", "SeventhOfNight": "2", "AngleBased": ""}
DENSE_MIN_LAT = 50.0
DENSE_WINDOWS = [(date(2026, 5, 1), date(2026, 8, 15)), (date(2026, 12, 1), date(2027, 1, 10))]
SOURCE = "AlAdhan API v1 (api.aladhan.com), Meezaan/PrayTimes engine, retrieved {}"


def daterange(a, b):
    while a <= b:
        yield a
        a += timedelta(days=1)


def make_tasks():
    tasks = {}

    def add(city, country, lat, lon, tz, asr, d, rule):
        school = dict(base.ASR_VARIANTS)[asr]
        t = {"city": city, "country": country, "lat": lat, "lon": lon, "tz": tz,
             "method": "MuslimWorldLeague", "method_id": base.METHODS["MuslimWorldLeague"],
             "asr": asr, "school": school, "date": d.isoformat(),
             "rule": rule, "hl": RULES[rule]}
        tasks[(city, asr, t["date"], rule)] = t

    d0 = date(2026, 1, 1)
    for (c, k, la, lo, tz) in LADDER:
        for asr, _ in base.ASR_VARIANTS:
            for i in range(base.N_DATES):
                add(c, k, la, lo, tz, asr, d0 + timedelta(days=base.STEP_DAYS * i), "AngleBased")
    for (c, k, la, lo, tz) in RULE_CITIES:
        for rule in ("MiddleOfNight", "SeventhOfNight"):
            for i in range(base.N_DATES):
                add(c, k, la, lo, tz, "Standard", d0 + timedelta(days=base.STEP_DAYS * i), rule)
    for (c, k, la, lo, tz) in {x[0]: x for x in LADDER + RULE_CITIES}.values():
        if la < DENSE_MIN_LAT:
            continue
        for (a, b) in DENSE_WINDOWS:
            for d in daterange(a, b):
                add(c, k, la, lo, tz, "Standard", d, "AngleBased")
                if c in {x[0] for x in RULE_CITIES}:
                    for rule in ("MiddleOfNight", "SeventhOfNight"):
                        add(c, k, la, lo, tz, "Standard", d, rule)
    return sorted(tasks.values(), key=lambda t: (t["city"], t["rule"], t["asr"], t["date"]))


def build(tasks, raw_dir, out_path, retrieved):
    records = []
    for t in tasks:
        j = json.load(open(base.task_raw_path(raw_dir, t)))
        data = j["data"]
        tm = data["timings"]
        for api_name, _ in base.PRAYERS:
            if not base.TIME_RE.match(tm[api_name]):
                sys.exit(f"bad timing {tm[api_name]!r} for {t}")
        hij = data["date"]["hijri"]
        rec = {"city": t["city"], "country": t["country"], "lat": t["lat"], "lon": t["lon"],
               "tz": t["tz"], "date": t["date"], "method": t["method"], "method_id": t["method_id"],
               "asr_school": t["asr"], "hijri_day": int(hij["day"]),
               "hijri_month_number": int(hij["month"]["number"]),
               "utc_offset": base.utc_offset_at_noon(t["tz"], t["date"])}
        for api_name, key in base.PRAYERS:
            rec[key] = tm[api_name]
        rec["ramadan"] = False
        rec["high_lat_rule"] = t["rule"]
        rec["source"] = SOURCE.format(retrieved)
        records.append(rec)
    out = {
        "source": SOURCE.format(retrieved) + ". Same conventions as the baseline dataset; per-record high_lat_rule "
                  "(AlAdhan latitudeAdjustmentMethod 1 = MiddleOfNight, 2 = SeventhOfNight, 3 = AngleBased).",
        "year": 2026, "date_step_days": base.STEP_DAYS, "record_count": len(records),
        "prayers_compared": ["fajr", "sunrise", "dhuhr", "asr", "maghrib", "isha"],
        "time_format": "HH:MM 24h local wall clock at record location (rounded to minute with +30s)",
        "records": records,
        "note": "Extended AlAdhan set: latitude ladder, non-default high-latitude rules, daily solstice/clamp windows.",
    }
    with open(out_path, "w") as f:
        json.dump(out, f)
    print(f"wrote {out_path} ({len(records)} records, {os.path.getsize(out_path)//1024} KB)")


def main():
    a = sys.argv[1:]
    opt = lambda k, d: a[a.index(k) + 1] if k in a else d
    raw_dir = opt("--raw-dir", base.RAW_DIR)
    out = opt("--out", DEFAULT_OUT)
    workers = int(opt("--workers", "4"))
    retrieved = opt("--retrieved", date.today().isoformat())
    tasks = make_tasks()
    base.download_all(tasks, raw_dir, workers)
    build(tasks, raw_dir, out, retrieved)


if __name__ == "__main__":
    main()
