using System;
using System.Collections.Generic;
using System.Globalization;
using GamePause.SalahEvents.PrayerTimes.Astronomical;
using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;
using GamePause.SalahEvents.PrayerTimes;

namespace GamePause.SalahEvents.PrayerTimeAccuracy
{
    /// <summary>
    /// Drives the Game Pause prayer-time calculation module over a reference dataset
    /// and measures accuracy of the resulting prayer times.
    ///
    /// The engine calls the real module under test (<see cref="PrayerTimeCalculator"/>),
    /// configured with the same policies as the reference:
    ///
    ///  1. UmmAlQura Ramadan rule - the reference (AlAdhan/PrayTimes) uses
    ///     Isha = Maghrib + 120 min during Hijri month 9 (Ramaḍān) instead of 90.
    ///     The dataset's Ramadan flag is passed to the calculator.
    ///  2. High-latitude handling - the reference uses the AngleBased rule.
    ///
    /// Rounding matches the reference: +30 s, then truncate to the minute.
    /// Local time = UTC + record.utc_offset (offset at local noon of that date).
    /// </summary>
    public static class PrayerTimeAccuracyEngine
    {
        private static readonly string[] PrayerKeys =
            { "fajr", "sunrise", "dhuhr", "asr", "maghrib", "isha" };

        /// <summary>Runs the full accuracy evaluation over every record in the dataset.</summary>
        public static AccuracyReport Evaluate(PrayerTimeAccuracyDataset dataset)
        {
            var report = new AccuracyReport();
            var methodAgg = new Dictionary<string, MethodAggregate>();
            var prayerAgg = new PerPrayerStat[6];
            for (int i = 0; i < 6; i++)
                prayerAgg[i] = new PerPrayerStat { prayer = PrayerKeys[i] };

            foreach (var rec in dataset.records)
            {
                bool hadInvalid;
                int[] ourMinutes = ComputeRecord(rec, out hadInvalid);
                report.total_records++;
                if (hadInvalid) report.invalid_calculated_times++;

                MethodAggregate agg;
                if (!methodAgg.TryGetValue(rec.method, out agg))
                    agg = methodAgg[rec.method] = new MethodAggregate();

                for (int i = 0; i < 6; i++)
                {
                    string key = PrayerKeys[i];
                    int refMin = ParseHhMm(rec, key);
                    int ourMin = ourMinutes[i];

                    var ps = prayerAgg[i];
                    ps.n++;
                    agg.n++;

                    int diff = ourMin - refMin;
                    int adiff = Math.Abs(diff);

                    report.total_comparisons++;
                    if (adiff <= 1) { report.within_1_min++; ps.within1++; }
                    if (adiff <= 2) { report.within_2_min++; ps.within2++; }
                    if (adiff <= 5) report.within_5_min++;
                    if (adiff > report.max_abs_error_min) report.max_abs_error_min = adiff;

                    ps.mean_diff_min += diff;
                    ps.sq += (double)diff * diff;
                    if (diff < ps.min_diff) ps.min_diff = diff;
                    if (diff > ps.max_diff) ps.max_diff = diff;

                    agg.absSum += adiff;
                    if (adiff <= 1) agg.w1++;
                    if (adiff <= 2) agg.w2++;
                    if (adiff > agg.maxAbs) agg.maxAbs = adiff;

                    if (adiff >= 2)
                    {
                        report.outliers.Add(new OutlierEntry
                        {
                            city = rec.city,
                            date = rec.date,
                            method = rec.method,
                            asr_school = rec.asr_school,
                            prayer = key,
                            reference_minutes = refMin,
                            calculated_minutes = ourMin,
                            diff_minutes = diff
                        });
                    }
                }
            }

            for (int i = 0; i < 6; i++)
            {
                var ps = prayerAgg[i];
                if (ps.n > 0)
                {
                    ps.mean_diff_min /= ps.n;
                    ps.std_diff_min = Math.Sqrt(Math.Max(0.0, ps.sq / ps.n - ps.mean_diff_min * ps.mean_diff_min));
                    ps.pct_within_1_min = 100.0 * ps.within1 / ps.n;
                    ps.pct_within_2_min = 100.0 * ps.within2 / ps.n;
                }
                report.per_prayer[i] = ps;
            }

            long totalAbs = 0;
            int totalCmps = 0;
            foreach (var kv in methodAgg)
            {
                var a = kv.Value;
                report.per_method.Add(new PerMethodStat
                {
                    method = kv.Key,
                    n = a.n,
                    mean_abs_diff_min = a.n > 0 ? (double)a.absSum / a.n : 0.0,
                    pct_within_1_min = a.n > 0 ? 100.0 * a.w1 / a.n : 0.0,
                    pct_within_2_min = a.n > 0 ? 100.0 * a.w2 / a.n : 0.0,
                    max_abs_diff = a.maxAbs
                });
                totalAbs += a.absSum;
                totalCmps += a.n;
            }
            report.per_method.Sort((x, y) => string.CompareOrdinal(x.method, y.method));
            report.mean_abs_error_min = totalCmps > 0 ? (double)totalAbs / totalCmps : 0.0;
            return report;
        }

        private class MethodAggregate
        {
            public int n;
            public long absSum;
            public int w1, w2;
            public int maxAbs;
        }

        /// <summary>
        /// Computes the six prayer times for one record with <see cref="PrayerTimeCalculator"/>,
        /// configured like the reference (AngleBased high-latitude rule, dataset Ramadan flag).
        /// Returns local minutes (0..1439) per prayer in fajr..isha order.
        /// <paramref name="hadInvalid"/> is true when the raw Fajr/Isha angle is never reached
        /// and the high-latitude rule had to supply the time.
        /// </summary>
        public static int[] ComputeRecord(PrayerTimeAccuracyRecord rec, out bool hadInvalid)
        {
            string[] parts = rec.date.Split('-');
            DateTime date = new DateTime(
                int.Parse(parts[0], CultureInfo.InvariantCulture),
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                int.Parse(parts[2], CultureInfo.InvariantCulture),
                0, 0, 0, DateTimeKind.Utc);

            CalculationMethod method = Enum.Parse<CalculationMethod>(rec.method);
            AsrJuristicMethod asrMethod = string.Equals(rec.asr_school, "Hanafi", StringComparison.OrdinalIgnoreCase)
                ? AsrJuristicMethod.Hanafi
                : AsrJuristicMethod.Standard;

            var calculator = new PrayerTimeCalculator(method, asrMethod, HighLatitudeRule.AngleBased);
            DailyPrayerTimes times = calculator.Calculate(date, new GeographicCoordinates(rec.lat, rec.lon), rec.ramadan);

            hadInvalid = IsAngleUnreached(calculator.Parameters.MethodParameters, date, rec);

            var result = new int[6];
            for (int i = 0; i < 6; i++)
            {
                result[i] = ToLocalMinutes((times.Get((PrayerType)i) - date).TotalHours, rec.utc_offset);
            }
            return result;
        }

        private static bool IsAngleUnreached(MethodParameters p, DateTime date, PrayerTimeAccuracyRecord rec)
        {
            double jd = JulianDateCalculator.ToJulianDayStartOfDay(date);
            if (double.IsNaN(SolarCalculator.GetTimeForAngle(jd, rec.lat, rec.lon, p.FajrAngle, true)))
                return true;
            return !p.UsesIshaInterval &&
                   double.IsNaN(SolarCalculator.GetTimeForAngle(jd, rec.lat, rec.lon, p.IshaAngle.Value, false));
        }

        /// <summary>fixHour: wrap to [0, 24).</summary>
        public static double FixHour(double t)
        {
            t %= 24.0;
            if (t < 0.0) t += 24.0;
            return t;
        }

        /// <summary>UTC hours -&gt; local minutes with reference rounding (+30 s, truncate).</summary>
        public static int ToLocalMinutes(double utcHours, double utcOffsetHours)
        {
            double local = FixHour(utcHours + utcOffsetHours + 0.5 / 60.0);
            return (int)Math.Floor(local * 60.0 + 1e-9) % 1440;
        }

        /// <summary>Parses "HH:MM" from a record into minutes-of-day.</summary>
        public static int ParseHhMm(PrayerTimeAccuracyRecord rec, string key)
        {
            string s;
            switch (key)
            {
                case "fajr": s = rec.fajr; break;
                case "sunrise": s = rec.sunrise; break;
                case "dhuhr": s = rec.dhuhr; break;
                case "asr": s = rec.asr; break;
                case "maghrib": s = rec.maghrib; break;
                default: s = rec.isha; break;
            }
            int colon = s.IndexOf(':');
            return int.Parse(s.Substring(0, colon), CultureInfo.InvariantCulture) * 60
                 + int.Parse(s.Substring(colon + 1), CultureInfo.InvariantCulture);
        }
    }
}
