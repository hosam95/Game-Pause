using System;
using System.Globalization;
using GamePause.SalahEvents.PrayerTimes.Astronomical;
using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;
using GamePause.SalahEvents.PrayerTimes.Validation;

namespace GamePause.SalahEvents.PrayerTimes
{
    /// <summary>
    /// Calculates the daily prayer times for a location and date.
    /// Every prayer always gets a time: when the sun never reaches a required angle,
    /// the time is estimated (see <see cref="HighLatitudeRule"/> and <see cref="PolarEstimationRule"/>)
    /// and flagged in the result.
    /// Holds only immutable parameters, so one instance can be reused for any number of calculations.
    /// </summary>
    public sealed class PrayerTimeCalculator
    {
        private const int RamadanMonth = 9;
        private const int UmmAlQuraRamadanIshaMinutes = 120;
        private const double FiqhCouncilReferenceLatitude = 45.0;
        private const double MeccaLatitude = 21.4225;
        private const double NearestLatitudeStep = 0.5;

        private static readonly ParametersValidator ParametersValidator = new ParametersValidator();
        private static readonly CoordinatesValidator CoordinatesValidator = new CoordinatesValidator();
        private static readonly UmAlQuraCalendar HijriCalendar = new UmAlQuraCalendar();

        public CalculationParameters Parameters { get; }

        /// <exception cref="ArgumentException">Thrown when the parameters are invalid.</exception>
        public PrayerTimeCalculator(CalculationParameters parameters)
        {
            ValidationResult validation = ParametersValidator.Validate(parameters);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.GetErrorMessage(), nameof(parameters));
            }

            Parameters = parameters;
        }

        /// <summary>
        /// Creates a calculator using the angles of a predefined calculation method.
        /// </summary>
        public PrayerTimeCalculator(
            CalculationMethod method,
            AsrJuristicMethod asrMethod = AsrJuristicMethod.Standard,
            HighLatitudeRule highLatitudeRule = HighLatitudeRule.AngleBased,
            PrayerTimeAdjustments? adjustments = null,
            PolarEstimationRule polarEstimationRule = PolarEstimationRule.ReferenceLatitude,
            ShiaMarja shiaMarja = ShiaMarja.Khamenei)
            : this(CalculationParameters.ForMethod(
                method, asrMethod, highLatitudeRule, adjustments, polarEstimationRule, shiaMarja))
        {
        }

        /// <summary>
        /// Calculates the prayer times for a calendar date at the given location.
        /// </summary>
        /// <param name="date">Local calendar date. Only the year, month and day are used.</param>
        /// <param name="coordinates">Observer's location.</param>
        /// <param name="isRamadan">
        /// Whether the date falls in Ramadan (Umm Al-Qura then uses a 120-minute Isha interval).
        /// Null detects it with the Umm Al-Qura calendar.
        /// </param>
        /// <returns>All six times in UTC, with estimated times flagged.</returns>
        /// <exception cref="ArgumentException">Thrown when the coordinates are out of range.</exception>
        public DailyPrayerTimes Calculate(DateTime date, GeographicCoordinates coordinates, bool? isRamadan = null)
        {
            ValidationResult validation = CoordinatesValidator.Validate(coordinates);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.GetErrorMessage(), nameof(coordinates));
            }

            DateTime day = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            double julianDay = JulianDateCalculator.ToJulianDayStartOfDay(day);
            bool ramadan = isRamadan ?? IsRamadan(day);

            DayTimes times = ComputeDay(julianDay, coordinates.Latitude, coordinates.Longitude, ramadan);
            if (times.HasMissingTime)
            {
                DayTimes reference = times.IsPolar
                    ? ComputePolarReferenceDay(julianDay, coordinates, ramadan)
                    : ComputeTwilightReferenceDay(julianDay, coordinates, ramadan);
                times.EstimateMissingFrom(reference);
                KeepIshaAfterMaghrib(times);
            }
            CombineIntervalIshaPastFajr(times, Parameters.MethodParameters);

            PrayerTimeAdjustments adjust = Parameters.Adjustments;
            return new DailyPrayerTimes(
                day,
                coordinates,
                ToUtc(day, times.Get(PrayerType.Fajr), adjust.Fajr),
                ToUtc(day, times.Get(PrayerType.Sunrise), adjust.Sunrise),
                ToUtc(day, times.Get(PrayerType.Dhuhr), adjust.Dhuhr),
                ToUtc(day, times.Get(PrayerType.Asr), adjust.Asr),
                ToUtc(day, times.Get(PrayerType.Maghrib), adjust.Maghrib),
                ToUtc(day, times.Get(PrayerType.Isha), adjust.Isha),
                times.Estimated);
        }

        /// <summary>
        /// Raw times for one latitude, after the high-latitude rule. NaN marks a time the sun never reaches.
        /// </summary>
        private DayTimes ComputeDay(double julianDay, double lat, double lon, bool isRamadan)
        {
            MethodParameters p = Parameters.MethodParameters;

            // All times are hours after 00:00 UTC of the calculation date.
            double sunrise = SolarCalculator.GetSunrise(julianDay, lat, lon);
            double sunset = SolarCalculator.GetSunset(julianDay, lat, lon);
            double dhuhr = SolarCalculator.GetSolarNoon(julianDay, lon);
            double asr = SolarCalculator.GetAsrTime(julianDay, lat, lon, (int)Parameters.AsrMethod);
            double fajr = SolarCalculator.GetTimeForAngle(julianDay, lat, lon, p.FajrAngle, true);

            double maghrib = sunset;
            if (p.UsesMaghribInterval)
            {
                maghrib = sunset + p.MaghribMinutes.Value / 60.0;
            }
            else if (p.MaghribAngle > 0)
            {
                maghrib = SolarCalculator.GetTimeForAngle(julianDay, lat, lon, p.MaghribAngle, false);
            }

            double isha;
            if (p.UsesIshaInterval)
            {
                int ishaMinutes = Parameters.Method == CalculationMethod.UmmAlQura && isRamadan
                    ? UmmAlQuraRamadanIshaMinutes
                    : p.IshaIntervalMinutes.Value;
                isha = maghrib + ishaMinutes / 60.0;
            }
            else
            {
                isha = SolarCalculator.GetTimeForAngle(julianDay, lat, lon, p.IshaAngle.Value, false);
            }

            var times = new DayTimes(fajr, sunrise, dhuhr, asr, maghrib, isha, sunset);
            ApplyHighLatitudeRule(times, p);
            return times;
        }

        /// <summary>
        /// Limits Fajr and Isha to a portion of the night (sunset to sunrise) when twilight lasts
        /// too long or never ends (see <see cref="HighLatitudeRule"/> for each rule's source).
        /// Maghrib is always the local astronomical time. Isha is kept no earlier than Maghrib.
        /// <see cref="HighLatitudeRule.NightFractionAt45"/> and <see cref="HighLatitudeRule.None"/>
        /// leave unreached times missing; <see cref="Calculate"/> estimates them.
        /// </summary>
        private void ApplyHighLatitudeRule(DayTimes times, MethodParameters p)
        {
            HighLatitudeRule rule = Parameters.HighLatitudeRule;
            double sunrise = times.Get(PrayerType.Sunrise);
            double sunset = times.Sunset;
            if (rule == HighLatitudeRule.None || rule == HighLatitudeRule.NightFractionAt45 ||
                double.IsNaN(sunrise) || double.IsNaN(sunset))
            {
                return;
            }

            double night = WrapHours(sunrise - sunset);

            double fajrPortion = NightPortion(rule, p.FajrAngle) * night;
            double fajr = times.Get(PrayerType.Fajr);
            if (double.IsNaN(fajr) || WrapHours(sunrise - fajr) > fajrPortion)
            {
                times.SetEstimated(PrayerType.Fajr, sunrise - fajrPortion);
            }

            if (!p.UsesIshaInterval)
            {
                double ishaPortion = NightPortion(rule, p.IshaAngle.Value) * night;
                double isha = times.Get(PrayerType.Isha);
                if (double.IsNaN(isha) || WrapHours(isha - sunset) > ishaPortion)
                {
                    times.SetEstimated(PrayerType.Isha, sunset + ishaPortion);
                }
            }

            KeepIshaAfterMaghrib(times);
        }

        /// <summary>
        /// An angle-based Maghrib (Tehran, Qom) can fall after a limited or estimated Isha;
        /// Isha cannot precede Maghrib, so it is moved to Maghrib.
        /// </summary>
        private static void KeepIshaAfterMaghrib(DayTimes times)
        {
            double sunset = times.Sunset;
            double maghrib = times.Get(PrayerType.Maghrib);
            double isha = times.Get(PrayerType.Isha);
            if (double.IsNaN(sunset) || double.IsNaN(maghrib) || double.IsNaN(isha))
            {
                return;
            }
            if (WrapHours(isha - sunset) < WrapHours(maghrib - sunset))
            {
                times.SetEstimated(PrayerType.Isha, maghrib);
            }
        }

        /// <summary>
        /// A fixed-interval Isha (e.g. Umm Al-Qura's 90/120 min) that would fall after that night's
        /// Fajr is combined with Maghrib (jamʿ taqdīm). Basis: the European Council for Fatwa and
        /// Research permits combining Maghrib and Isha where the time of Isha is lost to short nights.
        /// </summary>
        private static void CombineIntervalIshaPastFajr(DayTimes times, MethodParameters p)
        {
            double sunrise = times.Get(PrayerType.Sunrise);
            double sunset = times.Sunset;
            if (!p.UsesIshaInterval || double.IsNaN(sunrise) || double.IsNaN(sunset))
            {
                return;
            }

            double night = WrapHours(sunrise - sunset);
            double ishaOffset = WrapHours(times.Get(PrayerType.Isha) - sunset);
            double fajrOffset = night - WrapHours(sunrise - times.Get(PrayerType.Fajr));
            if (ishaOffset > fajrOffset)
            {
                times.SetEstimated(PrayerType.Isha, times.Get(PrayerType.Maghrib));
            }
        }

        /// <summary>
        /// Reference day for a date with sunrise and sunset whose Fajr / Isha / Maghrib angle is never reached.
        /// <see cref="HighLatitudeRule.NightFractionAt45"/> uses 45° (MWL decree, region 2);
        /// otherwise (<see cref="HighLatitudeRule.None"/>) the polar reference is used.
        /// </summary>
        private DayTimes ComputeTwilightReferenceDay(double julianDay, GeographicCoordinates coordinates, bool isRamadan)
        {
            if (Parameters.HighLatitudeRule == HighLatitudeRule.NightFractionAt45)
            {
                DayTimes day = ComputeDay(julianDay, Hemisphere(coordinates) * FiqhCouncilReferenceLatitude,
                    coordinates.Longitude, isRamadan);
                if (!day.HasMissingTime)
                {
                    return day;
                }
            }
            return ComputePolarReferenceDay(julianDay, coordinates, isRamadan);
        }

        /// <summary>
        /// Reference day on the same meridian (so solar noon stays local) for polar day or night:
        /// the marjaʿ's rule for the Shia methods, <see cref="PolarEstimationRule"/> otherwise.
        /// </summary>
        private DayTimes ComputePolarReferenceDay(double julianDay, GeographicCoordinates coordinates, bool isRamadan)
        {
            double lon = coordinates.Longitude;
            double hemisphere = Hemisphere(coordinates);

            if (Parameters.IsShiaMethod)
            {
                return Parameters.ShiaMarja == ShiaMarja.Sistani
                    ? ComputeNearestLatitudeDay(julianDay, coordinates, isRamadan, requireNoCorrection: false)
                    : ComputeLatitudeDayOrMecca(julianDay, hemisphere * FiqhCouncilReferenceLatitude, lon, isRamadan);
            }

            switch (Parameters.PolarEstimationRule)
            {
                case PolarEstimationRule.Mecca:
                    return ComputeDay(julianDay, MeccaLatitude, lon, isRamadan);

                case PolarEstimationRule.NearestNormalLatitude:
                    return ComputeNearestLatitudeDay(julianDay, coordinates, isRamadan, requireNoCorrection: true);

                case PolarEstimationRule.ReferenceLatitude:
                default:
                    double reference = Math.Min(Math.Abs(coordinates.Latitude), FiqhCouncilReferenceLatitude);
                    return ComputeLatitudeDayOrMecca(julianDay, hemisphere * reference, lon, isRamadan);
            }
        }

        /// <summary>
        /// Steps toward the equator on the same meridian until every time exists
        /// (and, with <paramref name="requireNoCorrection"/>, none needed a high-latitude correction).
        /// </summary>
        private DayTimes ComputeNearestLatitudeDay(
            double julianDay, GeographicCoordinates coordinates, bool isRamadan, bool requireNoCorrection)
        {
            double hemisphere = Hemisphere(coordinates);
            for (double refLat = Math.Abs(coordinates.Latitude) - NearestLatitudeStep; refLat > 0; refLat -= NearestLatitudeStep)
            {
                DayTimes candidate = ComputeDay(julianDay, hemisphere * refLat, coordinates.Longitude, isRamadan);
                if (requireNoCorrection ? candidate.IsNormal : !candidate.HasMissingTime)
                {
                    return candidate;
                }
            }
            return ComputeDay(julianDay, 0.0, coordinates.Longitude, isRamadan);
        }

        private DayTimes ComputeLatitudeDayOrMecca(double julianDay, double lat, double lon, bool isRamadan)
        {
            DayTimes day = ComputeDay(julianDay, lat, lon, isRamadan);
            // At or below 45° every angle is normally reached; fall back to Mecca's latitude if not.
            return day.HasMissingTime ? ComputeDay(julianDay, MeccaLatitude, lon, isRamadan) : day;
        }

        private static double Hemisphere(GeographicCoordinates coordinates) => coordinates.Latitude < 0 ? -1.0 : 1.0;

        private static double NightPortion(HighLatitudeRule rule, double angle)
        {
            switch (rule)
            {
                case HighLatitudeRule.AngleBased:
                    return angle / 60.0;
                case HighLatitudeRule.SeventhOfNight:
                    return 1.0 / 7.0;
                case HighLatitudeRule.MiddleOfNight:
                default:
                    return 0.5;
            }
        }

        private static bool IsRamadan(DateTime day)
        {
            if (day < HijriCalendar.MinSupportedDateTime || day > HijriCalendar.MaxSupportedDateTime)
            {
                return false;
            }
            return HijriCalendar.GetMonth(day) == RamadanMonth;
        }

        private static DateTime ToUtc(DateTime day, double hours, int adjustmentMinutes)
        {
            return day.AddHours(hours + adjustmentMinutes / 60.0);
        }

        /// <summary>Wraps an hour value into [0, 24).</summary>
        private static double WrapHours(double hours)
        {
            hours %= 24.0;
            return hours < 0 ? hours + 24.0 : hours;
        }

        /// <summary>
        /// Mutable working set of the six times (hours after 00:00 UTC) plus which ones are estimated.
        /// </summary>
        private sealed class DayTimes
        {
            private readonly double[] _hours;
            public bool[] Estimated { get; } = new bool[6];

            /// <summary>Astronomical sunset; differs from Maghrib for angle- or interval-based Maghrib.</summary>
            public double Sunset { get; }

            public DayTimes(double fajr, double sunrise, double dhuhr, double asr, double maghrib, double isha, double sunset)
            {
                _hours = new[] { fajr, sunrise, dhuhr, asr, maghrib, isha };
                Sunset = sunset;
            }

            public double Get(PrayerType prayer) => _hours[(int)prayer];

            public void SetEstimated(PrayerType prayer, double hours)
            {
                _hours[(int)prayer] = hours;
                Estimated[(int)prayer] = true;
            }

            public bool HasMissingTime => Array.Exists(_hours, double.IsNaN);

            /// <summary>No sunrise, sunset or Asr: polar day or night.</summary>
            public bool IsPolar => double.IsNaN(Get(PrayerType.Sunrise)) || double.IsNaN(Sunset) || double.IsNaN(Get(PrayerType.Asr));

            /// <summary>Every time was reached astronomically, without any high-latitude correction.</summary>
            public bool IsNormal => !HasMissingTime && Array.IndexOf(Estimated, true) < 0;

            /// <summary>
            /// Fills the missing times from a reference day.
            /// With a local sunrise and sunset, a missing Fajr / Maghrib / Isha takes the same fraction
            /// of the local night that it occupies in the reference night ("analogy with the nearest
            /// place where the time is marked"), so it always falls inside the local night and in order.
            /// A fixed-interval Isha is kept as the method prescribes, even if the night is shorter.
            /// Without them (polar day or night) the whole day is taken from the reference,
            /// as the Fiqh Council resolution prescribes.
            /// </summary>
            public void EstimateMissingFrom(DayTimes reference)
            {
                double sunrise = Get(PrayerType.Sunrise);

                if (IsPolar)
                {
                    foreach (PrayerType prayer in new[]
                             { PrayerType.Fajr, PrayerType.Sunrise, PrayerType.Asr, PrayerType.Maghrib, PrayerType.Isha })
                    {
                        SetEstimated(prayer, reference.Get(prayer));
                    }
                    return;
                }

                double refSunrise = reference.Get(PrayerType.Sunrise);
                double refMaghrib = reference.Get(PrayerType.Maghrib);

                if (double.IsNaN(Get(PrayerType.Maghrib)))
                {
                    double fraction = WrapHours(refMaghrib - reference.Sunset) / WrapHours(refSunrise - reference.Sunset);
                    SetEstimated(PrayerType.Maghrib, Sunset + fraction * WrapHours(sunrise - Sunset));
                }

                // Fajr and Isha share the window from Maghrib to the next sunrise, so an estimated
                // Isha can never fall before Maghrib or after the estimated Fajr.
                double maghrib = Get(PrayerType.Maghrib);
                double window = WrapHours(sunrise - maghrib);
                double refWindow = WrapHours(refSunrise - refMaghrib);

                if (double.IsNaN(Get(PrayerType.Fajr)))
                {
                    double fraction = WrapHours(refSunrise - reference.Get(PrayerType.Fajr)) / refWindow;
                    SetEstimated(PrayerType.Fajr, sunrise - fraction * window);
                }
                if (double.IsNaN(Get(PrayerType.Isha)))
                {
                    double fraction = WrapHours(reference.Get(PrayerType.Isha) - refMaghrib) / refWindow;
                    SetEstimated(PrayerType.Isha, maghrib + fraction * window);
                }
            }
        }
    }
}
