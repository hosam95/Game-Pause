using System;
using NUnit.Framework;
using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Tests
{
    [TestFixture]
    public class PrayerTimeCalculatorTests
    {
        private static readonly DateTime CairoDate = new DateTime(2026, 10, 9);
        private static readonly GeographicCoordinates Cairo = new GeographicCoordinates(30.0444, 31.2357);
        private static readonly GeographicCoordinates London = new GeographicCoordinates(51.5074, -0.1278);
        private static readonly GeographicCoordinates Jakarta = new GeographicCoordinates(-6.2088, 106.8456);
        private static readonly GeographicCoordinates Mecca = new GeographicCoordinates(21.4225, 39.8262);
        private static readonly GeographicCoordinates Tehran = new GeographicCoordinates(35.6892, 51.3890);

        // ----- Reference values (verified UTC, tolerance ±2 minutes) -----

        [Test]
        public void Calculate_EgyptianAuthority_Cairo20261009_FajrIs0226Utc()
        {
            AssertWithin2Minutes(NewCairoTimes().Fajr, CairoDate, 2, 26);
        }

        [Test]
        public void Calculate_EgyptianAuthority_Cairo20261009_SunriseIs0352Utc()
        {
            AssertWithin2Minutes(NewCairoTimes().Sunrise, CairoDate, 3, 52);
        }

        [Test]
        public void Calculate_EgyptianAuthority_Cairo20261009_DhuhrIs0942Utc()
        {
            AssertWithin2Minutes(NewCairoTimes().Dhuhr, CairoDate, 9, 42);
        }

        [Test]
        public void Calculate_EgyptianAuthority_Cairo20261009_AsrIs1302Utc()
        {
            AssertWithin2Minutes(NewCairoTimes().Asr, CairoDate, 13, 2);
        }

        [Test]
        public void Calculate_EgyptianAuthority_Cairo20261009_MaghribIs1531Utc()
        {
            AssertWithin2Minutes(NewCairoTimes().Maghrib, CairoDate, 15, 31);
        }

        [Test]
        public void Calculate_EgyptianAuthority_Cairo20261009_IshaIs1649Utc()
        {
            AssertWithin2Minutes(NewCairoTimes().Isha, CairoDate, 16, 49);
        }

        // ----- Ordering and method comparison -----

        [Test]
        public void Calculate_MidLatitudeCities_TimesAreInStrictlyChronologicalOrder()
        {
            AssertChronological(new PrayerTimeCalculator(CalculationMethod.EgyptianAuthority)
                .Calculate(CairoDate, Cairo));
            AssertChronological(new PrayerTimeCalculator(CalculationMethod.MuslimWorldLeague)
                .Calculate(new DateTime(2026, 3, 15), London));
            AssertChronological(new PrayerTimeCalculator(CalculationMethod.MuslimWorldLeague)
                .Calculate(new DateTime(2026, 7, 1), Jakarta));
        }

        [Test]
        public void Calculate_HanafiAsr_AsrIsLaterThanStandardAsr()
        {
            DailyPrayerTimes standard = new PrayerTimeCalculator(CalculationMethod.EgyptianAuthority, AsrJuristicMethod.Standard)
                .Calculate(CairoDate, Cairo);
            DailyPrayerTimes hanafi = new PrayerTimeCalculator(CalculationMethod.EgyptianAuthority, AsrJuristicMethod.Hanafi)
                .Calculate(CairoDate, Cairo);

            Assert.Less(standard.Asr, hanafi.Asr,
                $"Hanafi Asr should be later than Standard Asr (Standard {standard.Asr:HH:mm}, Hanafi {hanafi.Asr:HH:mm})");
        }

        [Test]
        public void Calculate_Tehran_MaghribIsLaterThanMuslimWorldLeagueMaghrib()
        {
            DailyPrayerTimes tehran = new PrayerTimeCalculator(CalculationMethod.Tehran)
                .Calculate(CairoDate, Tehran);
            DailyPrayerTimes mwl = new PrayerTimeCalculator(CalculationMethod.MuslimWorldLeague)
                .Calculate(CairoDate, Tehran);

            Assert.Less(mwl.Maghrib, tehran.Maghrib,
                "Tehran Maghrib (4.5° below horizon) should be later than sunset-based Maghrib");
        }

        // ----- Adjustments -----

        [Test]
        public void Calculate_WithAdjustments_FajrShiftedBy5AndIshaShiftedByMinus3Minutes()
        {
            DailyPrayerTimes plain = new PrayerTimeCalculator(CalculationMethod.MuslimWorldLeague)
                .Calculate(CairoDate, Cairo);
            DailyPrayerTimes adjusted = new PrayerTimeCalculator(
                CalculationMethod.MuslimWorldLeague,
                AsrJuristicMethod.Standard,
                HighLatitudeRule.MiddleOfNight,
                new PrayerTimeAdjustments(fajr: 5, isha: -3))
                .Calculate(CairoDate, Cairo);

            Assert.AreEqual(plain.Fajr.AddMinutes(5), adjusted.Fajr, "Fajr should be shifted by exactly +5 min");
            Assert.AreEqual(plain.Isha.AddMinutes(-3), adjusted.Isha, "Isha should be shifted by exactly -3 min");
            Assert.AreEqual(plain.Sunrise, adjusted.Sunrise, "Sunrise should be unchanged");
            Assert.AreEqual(plain.Dhuhr, adjusted.Dhuhr, "Dhuhr should be unchanged");
            Assert.AreEqual(plain.Asr, adjusted.Asr, "Asr should be unchanged");
            Assert.AreEqual(plain.Maghrib, adjusted.Maghrib, "Maghrib should be unchanged");
        }

        // ----- Umm Al-Qura Isha interval -----

        [Test]
        public void Calculate_UmmAlQura_NotRamadan_IshaIs90MinutesAfterMaghrib()
        {
            DailyPrayerTimes times = new PrayerTimeCalculator(CalculationMethod.UmmAlQura)
                .Calculate(CairoDate, Mecca, isRamadan: false);

            Assert.AreEqual(TimeSpan.FromMinutes(90), times.Isha - times.Maghrib);
        }

        [Test]
        public void Calculate_UmmAlQura_Ramadan_IshaIs120MinutesAfterMaghrib()
        {
            DailyPrayerTimes times = new PrayerTimeCalculator(CalculationMethod.UmmAlQura)
                .Calculate(CairoDate, Mecca, isRamadan: true);

            Assert.AreEqual(TimeSpan.FromMinutes(120), times.Isha - times.Maghrib);
        }

        [Test]
        public void Calculate_UmmAlQura_AutoDetect_RamadanDate20270220_Uses120MinuteInterval()
        {
            DailyPrayerTimes times = new PrayerTimeCalculator(CalculationMethod.UmmAlQura)
                .Calculate(new DateTime(2027, 2, 20), Mecca);

            Assert.AreEqual(TimeSpan.FromMinutes(120), times.Isha - times.Maghrib);
        }

        [Test]
        public void Calculate_UmmAlQura_AutoDetect_NonRamadanDate20270501_Uses90MinuteInterval()
        {
            DailyPrayerTimes times = new PrayerTimeCalculator(CalculationMethod.UmmAlQura)
                .Calculate(new DateTime(2027, 5, 1), Mecca);

            Assert.AreEqual(TimeSpan.FromMinutes(90), times.Isha - times.Maghrib);
        }

        // ----- Invalid input -----

        [Test]
        public void Calculate_InvalidCoordinates_ThrowsArgumentException()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(CalculationMethod.MuslimWorldLeague);

            Assert.Throws<ArgumentException>(
                () => calculator.Calculate(CairoDate, new GeographicCoordinates(91, 181)));
        }

        [Test]
        public void Constructor_InvalidParameters_ThrowsArgumentException()
        {
            CalculationParameters invalid = new CalculationParameters(
                CalculationMethod.EgyptianAuthority,
                new MethodParameters(fajrAngle: 5, ishaAngle: 17));

            Assert.Throws<ArgumentException>(() => new PrayerTimeCalculator(invalid));
        }

        // ----- Date handling -----

        [Test]
        public void Calculate_TimePartOfInputDate_ReturnsSameTimesRegardlessOfTime()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(CalculationMethod.EgyptianAuthority);

            DailyPrayerTimes startOfDay = calculator.Calculate(new DateTime(2026, 10, 9, 0, 0, 0), Cairo);
            DailyPrayerTimes endOfDay = calculator.Calculate(new DateTime(2026, 10, 9, 23, 59, 0), Cairo);

            Assert.AreEqual(startOfDay.Fajr, endOfDay.Fajr, "Fajr should be identical");
            Assert.AreEqual(startOfDay.Sunrise, endOfDay.Sunrise, "Sunrise should be identical");
            Assert.AreEqual(startOfDay.Dhuhr, endOfDay.Dhuhr, "Dhuhr should be identical");
            Assert.AreEqual(startOfDay.Asr, endOfDay.Asr, "Asr should be identical");
            Assert.AreEqual(startOfDay.Maghrib, endOfDay.Maghrib, "Maghrib should be identical");
            Assert.AreEqual(startOfDay.Isha, endOfDay.Isha, "Isha should be identical");
        }

        [Test]
        public void Calculate_WithInputDate_ResultDateEqualsInputCalendarDate()
        {
            DailyPrayerTimes times = new PrayerTimeCalculator(CalculationMethod.EgyptianAuthority)
                .Calculate(new DateTime(2026, 10, 9, 13, 45, 0), Cairo);

            Assert.AreEqual(new DateTime(2026, 10, 9), times.Date);
        }

        // ----- Helpers -----

        private static DailyPrayerTimes NewCairoTimes()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(CalculationMethod.EgyptianAuthority);
            return calculator.Calculate(CairoDate, Cairo);
        }

        private static void AssertWithin2Minutes(DateTime actual, DateTime day, int hour, int minute)
        {
            DateTime expected = day.AddHours(hour).AddMinutes(minute);
            double diffMinutes = (actual - expected).TotalMinutes;
            Assert.IsTrue(diffMinutes >= -2.0 && diffMinutes <= 2.0,
                $"Expected {hour:D2}:{minute:D2} UTC ±2 min but was {actual:HH:mm} UTC");
        }

        private static void AssertChronological(DailyPrayerTimes times)
        {
            Assert.Less(times.Fajr, times.Sunrise, "Fajr should be before Sunrise");
            Assert.Less(times.Sunrise, times.Dhuhr, "Sunrise should be before Dhuhr");
            Assert.Less(times.Dhuhr, times.Asr, "Dhuhr should be before Asr");
            Assert.Less(times.Asr, times.Maghrib, "Asr should be before Maghrib");
            Assert.Less(times.Maghrib, times.Isha, "Maghrib should be before Isha");
        }
    }
}
