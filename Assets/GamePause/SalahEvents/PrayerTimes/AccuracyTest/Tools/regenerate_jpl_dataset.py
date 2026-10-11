#!/usr/bin/env python3
"""
Build the INDEPENDENT reference dataset (Resources/prayer_times_dataset_jpl.json).

Prayer times are derived straight from the NASA/JPL DE421 planetary ephemeris (via Skyfield),
so they share no code or approximation with AlAdhan/PrayTimes or with the module under test.
Used to judge, on the same cities and dates as the AlAdhan ladder, which side is closer to the
astronomical truth (the AlAdhan afternoon-prayer spread, see README.md).

Definitions (MuslimWorldLeague: Fajr 18 deg, Isha 17 deg, Asr Standard / Hanafi):
    Dhuhr    solar transit (maximum altitude)
    Sunrise / Maghrib  geometric centre of the sun at -0.833 deg (refraction + semi-diameter)
    Fajr / Isha        sun centre at -18 / -17 deg below the horizon (airless geometry)
    Asr      altitude = atan(1 / (shadow_factor + tan|lat - declination|)), declination at transit
Times use the +30 s / truncate rounding of the reference. Days on which the Fajr or Isha angle
is never reached are skipped (no astronomical value exists; high-latitude rules are
a ruling, not astronomy). Coordinates and dates match the AlAdhan ladder in
regenerate_extended_dataset.py.

Requirements: pip install skyfield tzdata   (downloads de421.bsp, ~17 MB, from NASA/JPL on first run)

Usage:
    python regenerate_jpl_dataset.py [--out FILE] [--eph-dir DIR] [--retrieved YYYY-MM-DD]
"""
import json, math, os, sys
from datetime import date, datetime, timedelta, timezone

from skyfield.api import Loader, wgs84
import regenerate_dataset as base
from regenerate_extended_dataset import LADDER

DEFAULT_OUT = os.path.normpath(os.path.join(base.HERE, "..", "Resources", "prayer_times_dataset_jpl.json"))
# Cities that have both a JPL record and an AlAdhan ladder record; Cairo / Sydney add mid/southern latitudes.
EXTRA = [("Cairo", "Egypt", 30.0444, 31.2357, "Africa/Cairo"),
         ("Sydney", "Australia", -33.8688, 151.2093, "Australia/Sydney")]
SOURCE = "NASA/JPL DE421 ephemeris via Skyfield (independent of AlAdhan / PrayTimes), retrieved {}"


class Sky:
    def __init__(self, eph_dir):
        loader = Loader(eph_dir)
        self.ts = loader.timescale()
        self.eph = loader("de421.bsp")
        self.earth, self.sun = self.eph["earth"], self.eph["sun"]

    def alt_dec(self, lat, lon, t):
        site = self.earth + wgs84.latlon(lat, lon)
        app = site.at(t).observe(self.sun).apparent()
        alt, _, _ = app.altaz()               # geometric (no refraction) altitude of the sun centre
        dec = app.radec(epoch="date")[1]
        return alt.degrees, dec.degrees

    def alt(self, lat, lon, jd):
        return self.alt_dec(lat, lon, self.ts.tt_jd(jd))[0]


def bisect(f, a, b, n=30):
    fa = f(a)
    for _ in range(n):
        m = 0.5 * (a + b)
        fm = f(m)
        if (fa < 0) == (fm < 0):
            a, fa = m, fm
        else:
            b = m
    return 0.5 * (a + b)


def day_times(sky, lat, lon, d, asr_factor):
    """UTC Julian days of (fajr, sunrise, dhuhr, asr, maghrib, isha), or None when an angle is unreached."""
    # Local solar noon is near 12:00 - lon/15 UTC.
    jd0 = sky.ts.utc(d.year, d.month, d.day, 0).tt
    guess = jd0 + (12.0 - lon / 15.0) / 24.0
    # transit: maximise altitude within +/- 1 h of the guess
    lo, hi = guess - 1 / 24, guess + 1 / 24
    for _ in range(30):
        m1, m2 = lo + (hi - lo) / 3, hi - (hi - lo) / 3
        if sky.alt(lat, lon, m1) < sky.alt(lat, lon, m2):
            lo = m1
        else:
            hi = m2
    noon = 0.5 * (lo + hi)
    a_noon, dec = sky.alt_dec(lat, lon, sky.ts.tt_jd(noon))

    def cross(target, rising):
        f = lambda j: sky.alt(lat, lon, j) - target
        if rising:
            a, b = noon - 0.5, noon
        else:
            a, b = noon, noon + 0.5
        lo_v, hi_v = f(a), f(b)
        if (lo_v < 0) == (hi_v < 0):
            return None
        return bisect(f, a, b)

    asr_alt = math.degrees(math.atan(1.0 / (asr_factor + math.tan(math.radians(abs(lat - dec))))))
    out = [cross(-18.0, True), cross(-0.833, True), noon, cross(asr_alt, False), cross(-0.833, False), cross(-17.0, False)]
    return None if any(v is None for v in out) else out


def tt_to_local_minutes(sky, jd_tt, utc_offset):
    t = sky.ts.tt_jd(jd_tt)
    dt = t.utc_datetime()
    hours = dt.hour + dt.minute / 60 + dt.second / 3600 + dt.microsecond / 3.6e9
    local = (hours + utc_offset + 0.5 / 60.0) % 24.0
    return int(math.floor(local * 60.0 + 1e-9)) % 1440


def fmt(m):
    return f"{m // 60:02d}:{m % 60:02d}"


def main():
    a = sys.argv[1:]
    opt = lambda k, d: a[a.index(k) + 1] if k in a else d
    out_path = opt("--out", DEFAULT_OUT)
    eph_dir = opt("--eph-dir", os.getcwd())
    retrieved = opt("--retrieved", date.today().isoformat())
    sky = Sky(eph_dir)
    records, skipped = [], 0
    d0 = date(2026, 1, 1)
    for (city, country, lat, lon, tz) in LADDER + EXTRA:
        for asr_name, _ in base.ASR_VARIANTS:
            factor = 1.0 if asr_name == "Standard" else 2.0
            for i in range(base.N_DATES):
                d = d0 + timedelta(days=base.STEP_DAYS * i)
                times = day_times(sky, lat, lon, d, factor)
                if times is None:
                    skipped += 1
                    continue
                off = base.utc_offset_at_noon(tz, d.isoformat())
                mins = [tt_to_local_minutes(sky, j, off) for j in times]
                rec = {"city": city, "country": country, "lat": lat, "lon": lon, "tz": tz,
                       "date": d.isoformat(), "method": "MuslimWorldLeague",
                       "method_id": base.METHODS["MuslimWorldLeague"], "asr_school": asr_name,
                       "hijri_day": 0, "hijri_month_number": 0, "utc_offset": off}
                for key, m in zip(["fajr", "sunrise", "dhuhr", "asr", "maghrib", "isha"], mins):
                    rec[key] = fmt(m)
                rec["ramadan"] = False
                rec["high_lat_rule"] = "None"
                rec["source"] = SOURCE.format(retrieved)
                records.append(rec)
    out = {"source": SOURCE.format(retrieved) + ". Days on which the Fajr or Isha angle is never reached are omitted.",
           "year": 2026, "date_step_days": base.STEP_DAYS, "record_count": len(records),
           "prayers_compared": ["fajr", "sunrise", "dhuhr", "asr", "maghrib", "isha"],
           "time_format": "HH:MM 24h local wall clock (rounded with +30s, truncated)",
           "records": records,
           "note": f"{skipped} city/Asr/date combinations skipped because Fajr or Isha is astronomically unreachable."}
    with open(out_path, "w") as f:
        json.dump(out, f)
    print(f"wrote {out_path} ({len(records)} records, skipped {skipped})")


if __name__ == "__main__":
    main()
