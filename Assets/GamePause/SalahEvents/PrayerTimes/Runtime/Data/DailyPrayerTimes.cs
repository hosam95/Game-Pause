using System;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Data
{
    /// <summary>
    /// Prayer times for one calendar date and location. Immutable.
    /// All times are UTC and always present. A time is flagged as estimated when the sun
    /// does not reach the required angle that day and a high-latitude or polar rule supplied it.
    /// </summary>
    public sealed class DailyPrayerTimes
    {
        private static readonly PrayerType[] Order =
        {
            PrayerType.Fajr, PrayerType.Sunrise, PrayerType.Dhuhr,
            PrayerType.Asr, PrayerType.Maghrib, PrayerType.Isha
        };

        private readonly bool[] _estimated;

        /// <summary>The calendar date the times were calculated for.</summary>
        public DateTime Date { get; }
        public GeographicCoordinates Coordinates { get; }

        public DateTime Fajr { get; }
        public DateTime Sunrise { get; }
        public DateTime Dhuhr { get; }
        public DateTime Asr { get; }
        public DateTime Maghrib { get; }
        public DateTime Isha { get; }

        /// <summary>True when at least one time was estimated rather than observed astronomically.</summary>
        public bool HasEstimatedTimes => Array.IndexOf(_estimated, true) >= 0;

        /// <param name="estimated">Per-prayer estimated flags in <see cref="PrayerType"/> order; null means none.</param>
        public DailyPrayerTimes(
            DateTime date,
            GeographicCoordinates coordinates,
            DateTime fajr,
            DateTime sunrise,
            DateTime dhuhr,
            DateTime asr,
            DateTime maghrib,
            DateTime isha,
            bool[] estimated = null)
        {
            if (estimated != null && estimated.Length != Order.Length)
            {
                throw new ArgumentException($"Expected {Order.Length} flags.", nameof(estimated));
            }

            Date = date.Date;
            Coordinates = coordinates;
            Fajr = fajr;
            Sunrise = sunrise;
            Dhuhr = dhuhr;
            Asr = asr;
            Maghrib = maghrib;
            Isha = isha;
            _estimated = estimated != null ? (bool[])estimated.Clone() : new bool[Order.Length];
        }

        /// <summary>Gets the UTC time of a prayer.</summary>
        public DateTime Get(PrayerType prayer)
        {
            switch (prayer)
            {
                case PrayerType.Fajr: return Fajr;
                case PrayerType.Sunrise: return Sunrise;
                case PrayerType.Dhuhr: return Dhuhr;
                case PrayerType.Asr: return Asr;
                case PrayerType.Maghrib: return Maghrib;
                case PrayerType.Isha: return Isha;
                default: throw new ArgumentOutOfRangeException(nameof(prayer), prayer, null);
            }
        }

        /// <summary>Whether the time of a prayer was estimated by a high-latitude or polar rule.</summary>
        public bool IsEstimated(PrayerType prayer)
        {
            if ((int)prayer < 0 || (int)prayer >= _estimated.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(prayer), prayer, null);
            }
            return _estimated[(int)prayer];
        }

        /// <summary>Gets the time of a prayer converted to the given time zone.</summary>
        public DateTime GetLocal(PrayerType prayer, TimeZoneInfo timeZone)
        {
            if (timeZone == null) throw new ArgumentNullException(nameof(timeZone));
            return TimeZoneInfo.ConvertTimeFromUtc(Get(prayer), timeZone);
        }

        /// <summary>
        /// Finds the first time (Sunrise included) strictly after <paramref name="utcNow"/>.
        /// Returns false when all of this day's times have passed.
        /// </summary>
        public bool TryGetNext(DateTime utcNow, out PrayerType prayer, out DateTime time)
        {
            foreach (PrayerType candidate in Order)
            {
                DateTime t = Get(candidate);
                if (t > utcNow)
                {
                    prayer = candidate;
                    time = t;
                    return true;
                }
            }

            prayer = default;
            time = default;
            return false;
        }

        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd} {Coordinates} UTC | " +
                   $"Fajr {Format(PrayerType.Fajr)}, Sunrise {Format(PrayerType.Sunrise)}, Dhuhr {Format(PrayerType.Dhuhr)}, " +
                   $"Asr {Format(PrayerType.Asr)}, Maghrib {Format(PrayerType.Maghrib)}, Isha {Format(PrayerType.Isha)}";
        }

        private string Format(PrayerType prayer)
        {
            return Get(prayer).ToString("HH:mm") + (IsEstimated(prayer) ? "*" : "");
        }
    }
}
