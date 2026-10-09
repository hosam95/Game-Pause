using System;
using NUnit.Framework;
using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Tests
{
    /// <summary>
    /// Tests for the high-latitude (<see cref="HighLatitudeRule"/>) and polar
    /// (<see cref="PolarEstimationRule"/>, <see cref="ShiaMarja"/>) behavior of the calculator.
    /// </summary>
    [TestFixture]
    public class HighLatitudeTests
    {
        private static readonly GeographicCoordinates Cairo = new GeographicCoordinates(30.0444, 31.2357);
        private static readonly GeographicCoordinates Reykjavik = new GeographicCoordinates(64.1466, -21.9426);
        private static readonly GeographicCoordinates Tromso = new GeographicCoordinates(69.6492, 18.9553);
        private static readonly GeographicCoordinates Longyearbyen = new GeographicCoordinates(78.2232, 15.6267);
        private static readonly GeographicCoordinates McMurdo = new GeographicCoordinates(-77.85, 166.67);

        // ----- 1. Normal latitude: nothing is estimated -----

        [Test]
        public void Calculate_CairoInOctober_HasNoEstimatedTimes()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(CalculationMethod.EgyptianAuthority);

            DailyPrayerTimes times = calculator.Calculate(new DateTime(2026, 10, 9), Cairo);

            Assert.IsFalse(times.HasEstimatedTimes);
            AssertInOrder(times);
        }

        // ----- 2-4. Reykjavik at the summer solstice (twilight never ends) -----

        [Test]
        public void Calculate_ReykjavikSolsticeWithHighLatitudeNone_FajrAndIshaEstimatedAndInOrder()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(
                CalculationMethod.MuslimWorldLeague, highLatitudeRule: HighLatitudeRule.None);

            DailyPrayerTimes times = calculator.Calculate(new DateTime(2026, 6, 21), Reykjavik);

            Assert.IsTrue(times.IsEstimated(PrayerType.Fajr));
            Assert.IsTrue(times.IsEstimated(PrayerType.Isha));
            Assert.IsFalse(times.IsEstimated(PrayerType.Sunrise));
            Assert.IsFalse(times.IsEstimated(PrayerType.Dhuhr));
            Assert.IsFalse(times.IsEstimated(PrayerType.Asr));
            Assert.IsFalse(times.IsEstimated(PrayerType.Maghrib));
            AssertInOrder(times);
        }

        [Test]
        public void Calculate_ReykjavikSolsticeWithAngleBased_FajrAndIshaEstimatedAndInOrder()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(CalculationMethod.MuslimWorldLeague);

            DailyPrayerTimes times = calculator.Calculate(new DateTime(2026, 6, 21), Reykjavik);

            Assert.IsTrue(times.IsEstimated(PrayerType.Fajr));
            Assert.IsTrue(times.IsEstimated(PrayerType.Isha));
            AssertInOrder(times);
        }

        [Test]
        public void Calculate_ReykjavikSolsticeWithNightFractionAt45_FajrDiffersFromAngleBased()
        {
            PrayerTimeCalculator nightFraction = new PrayerTimeCalculator(
                CalculationMethod.MuslimWorldLeague, highLatitudeRule: HighLatitudeRule.NightFractionAt45);
            PrayerTimeCalculator angleBased = new PrayerTimeCalculator(CalculationMethod.MuslimWorldLeague);
            DateTime date = new DateTime(2026, 6, 21);

            DailyPrayerTimes times = nightFraction.Calculate(date, Reykjavik);
            DailyPrayerTimes angleBasedTimes = angleBased.Calculate(date, Reykjavik);

            Assert.IsTrue(times.IsEstimated(PrayerType.Fajr));
            Assert.IsTrue(times.IsEstimated(PrayerType.Isha));
            AssertInOrder(times);
            Assert.AreNotEqual(angleBasedTimes.Fajr, times.Fajr,
                "NightFractionAt45 must take the 45-degree fraction of the night, not angle/60");
        }

        // ----- 5. Longyearbyen at both solstices (polar day and polar night) -----

        [Test]
        public void Calculate_LongyearbyenBothSolstices_OnlyDhuhrIsNotEstimated()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(CalculationMethod.MuslimWorldLeague);
            DateTime[] solstices = { new DateTime(2026, 12, 21), new DateTime(2026, 6, 21) };

            foreach (DateTime date in solstices)
            {
                DailyPrayerTimes times = calculator.Calculate(date, Longyearbyen);

                Assert.IsFalse(times.IsEstimated(PrayerType.Dhuhr), date.ToString("yyyy-MM-dd"));
                Assert.IsTrue(times.IsEstimated(PrayerType.Fajr), date.ToString("yyyy-MM-dd"));
                Assert.IsTrue(times.IsEstimated(PrayerType.Sunrise), date.ToString("yyyy-MM-dd"));
                Assert.IsTrue(times.IsEstimated(PrayerType.Asr), date.ToString("yyyy-MM-dd"));
                Assert.IsTrue(times.IsEstimated(PrayerType.Maghrib), date.ToString("yyyy-MM-dd"));
                Assert.IsTrue(times.IsEstimated(PrayerType.Isha), date.ToString("yyyy-MM-dd"));
                AssertInOrder(times);
            }
        }

        // ----- 6-7. Polar results equal the reference-latitude results -----

        [Test]
        public void Calculate_LongyearbyenWinterWithReferenceLatitude_TimesEqualReferenceAt45Degrees()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(CalculationMethod.MuslimWorldLeague);
            DateTime date = new DateTime(2026, 12, 21);

            DailyPrayerTimes polar = calculator.Calculate(date, Longyearbyen);
            DailyPrayerTimes reference = calculator.Calculate(date, new GeographicCoordinates(45.0, 15.6267));

            AssertEqualTimes(polar, reference);
        }

        [Test]
        public void Calculate_LongyearbyenWinterWithMecca_TimesEqualReferenceAtMeccaLatitude()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(
                CalculationMethod.MuslimWorldLeague, polarEstimationRule: PolarEstimationRule.Mecca);
            DateTime date = new DateTime(2026, 12, 21);

            DailyPrayerTimes polar = calculator.Calculate(date, Longyearbyen);
            DailyPrayerTimes reference = calculator.Calculate(date, new GeographicCoordinates(21.4225, 15.6267));

            AssertEqualTimes(polar, reference);
        }

        [Test]
        public void Calculate_McMurdoMidnightSunWithReferenceLatitude_TimesEqualReferenceAt45DegreesSouth()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(CalculationMethod.MuslimWorldLeague);
            DateTime date = new DateTime(2026, 6, 21);

            DailyPrayerTimes polar = calculator.Calculate(date, McMurdo);
            DailyPrayerTimes reference = calculator.Calculate(date, new GeographicCoordinates(-45.0, 166.67));

            AssertEqualTimes(polar, reference);
        }

        // ----- 8. NearestNormalLatitude differs from ReferenceLatitude -----

        [Test]
        public void Calculate_LongyearbyenMidnightSunWithNearestNormalLatitude_InOrderAndDiffersFromReferenceLatitude()
        {
            PrayerTimeCalculator nearest = new PrayerTimeCalculator(
                CalculationMethod.MuslimWorldLeague, polarEstimationRule: PolarEstimationRule.NearestNormalLatitude);
            PrayerTimeCalculator reference = new PrayerTimeCalculator(CalculationMethod.MuslimWorldLeague);
            DateTime date = new DateTime(2026, 6, 21);

            DailyPrayerTimes nearestTimes = nearest.Calculate(date, Longyearbyen);
            DailyPrayerTimes referenceTimes = reference.Calculate(date, Longyearbyen);

            AssertInOrder(nearestTimes);
            Assert.IsFalse(AllSixTimesEqual(nearestTimes, referenceTimes),
                "The nearest normal latitude (below 45 degrees) must give different times than the 45-degree reference");
        }

        // ----- 9. Year sweep: every polar estimation rule keeps the times in order -----

        [Test]
        public void Calculate_EverySeventhDayOf2026ForHighLatitudePlaces_TimesAlwaysInOrder()
        {
            GeographicCoordinates[] places = { Reykjavik, Tromso, Longyearbyen };
            DateTime first = new DateTime(2026, 1, 1);

            for (int k = 0; ; k++)
            {
                DateTime date = first.AddDays(7 * k);
                if (date.Year != 2026)
                {
                    break;
                }

                foreach (GeographicCoordinates place in places)
                {
                    foreach (PolarEstimationRule rule in Enum.GetValues(typeof(PolarEstimationRule)))
                    {
                        PrayerTimeCalculator calculator = new PrayerTimeCalculator(
                            CalculationMethod.MuslimWorldLeague, polarEstimationRule: rule);

                        DailyPrayerTimes times = calculator.Calculate(date, place);
                        AssertInOrder(times);
                    }
                }
            }
        }

        // ----- 10. Umm Al-Qura's fixed-interval Isha combined with Maghrib -----

        [Test]
        public void Calculate_UmmAlQuraTromsoOnShortNight_IshaCombinedWithMaghrib()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(
                CalculationMethod.UmmAlQura, highLatitudeRule: HighLatitudeRule.None);

            DailyPrayerTimes times = calculator.Calculate(new DateTime(2026, 5, 16), Tromso, isRamadan: false);

            Assert.AreEqual(times.Maghrib, times.Isha,
                "A 90-minute Isha that would fall past Fajr must be combined with Maghrib");
            Assert.IsTrue(times.IsEstimated(PrayerType.Isha));
        }

        [Test]
        public void Calculate_UmmAlQuraTromsoOnNormalNight_IshaIsNinetyMinutesAfterMaghrib()
        {
            PrayerTimeCalculator calculator = new PrayerTimeCalculator(
                CalculationMethod.UmmAlQura, highLatitudeRule: HighLatitudeRule.None);

            DailyPrayerTimes times = calculator.Calculate(new DateTime(2026, 3, 20), Tromso, isRamadan: false);

            Assert.AreEqual(TimeSpan.FromMinutes(90), times.Isha - times.Maghrib);
            Assert.IsFalse(times.IsEstimated(PrayerType.Isha));
            Assert.IsFalse(times.HasEstimatedTimes);
        }

        // ----- 11. Shia methods never shorten Maghrib -----

        [Test]
        public void Calculate_TehranReykjavikWithAngleBased_MaghribIsNeverShortened()
        {
            PrayerTimeCalculator angleBased = new PrayerTimeCalculator(CalculationMethod.Tehran);
            PrayerTimeCalculator noRule = new PrayerTimeCalculator(
                CalculationMethod.Tehran, highLatitudeRule: HighLatitudeRule.None);
            DateTime date = new DateTime(2026, 5, 10);

            DailyPrayerTimes times = angleBased.Calculate(date, Reykjavik);
            DailyPrayerTimes noRuleTimes = noRule.Calculate(date, Reykjavik);

            Assert.IsFalse(times.IsEstimated(PrayerType.Maghrib));
            Assert.AreEqual(noRuleTimes.Maghrib, times.Maghrib,
                "Maghrib must be the local astronomical time for the Shia methods");
            Assert.IsTrue(times.IsEstimated(PrayerType.Isha));
            Assert.LessOrEqual(times.Maghrib, times.Isha);
        }

        // ----- 12. ShiaMarja polar references -----

        [Test]
        public void Calculate_TehranLongyearbyenWinterWithKhameneiOrMakarem_TimesEqualReferenceAt45Degrees()
        {
            PrayerTimeCalculator khamenei = new PrayerTimeCalculator(
                CalculationMethod.Tehran, shiaMarja: ShiaMarja.Khamenei);
            PrayerTimeCalculator makarem = new PrayerTimeCalculator(
                CalculationMethod.Tehran, shiaMarja: ShiaMarja.Makarem);
            DateTime date = new DateTime(2026, 12, 21);

            DailyPrayerTimes khameneiTimes = khamenei.Calculate(date, Longyearbyen);
            DailyPrayerTimes makaremTimes = makarem.Calculate(date, Longyearbyen);
            DailyPrayerTimes reference = khamenei.Calculate(date, new GeographicCoordinates(45.0, 15.6267));

            AssertEqualTimes(khameneiTimes, makaremTimes);
            AssertEqualTimes(khameneiTimes, reference);
        }

        [Test]
        public void Calculate_TehranLongyearbyenWinterWithSistani_InOrderAndDiffersFromKhamenei()
        {
            PrayerTimeCalculator sistani = new PrayerTimeCalculator(
                CalculationMethod.Tehran, shiaMarja: ShiaMarja.Sistani);
            PrayerTimeCalculator khamenei = new PrayerTimeCalculator(
                CalculationMethod.Tehran, shiaMarja: ShiaMarja.Khamenei);
            DateTime date = new DateTime(2026, 12, 21);

            DailyPrayerTimes sistaniTimes = sistani.Calculate(date, Longyearbyen);
            DailyPrayerTimes khameneiTimes = khamenei.Calculate(date, Longyearbyen);

            AssertInOrder(sistaniTimes);
            Assert.IsFalse(AllSixTimesEqual(sistaniTimes, khameneiTimes),
                "Sistani uses the nearest place with a day and night, not the 45-degree reference");
        }

        // ----- 13. ShiaMarja has no effect on non-Shia methods -----

        [Test]
        public void Calculate_MuslimWorldLeagueWithAnyShiaMarja_TimesAreEqual()
        {
            DateTime date = new DateTime(2026, 12, 21);
            DailyPrayerTimes khamenei = new PrayerTimeCalculator(
                CalculationMethod.MuslimWorldLeague, shiaMarja: ShiaMarja.Khamenei).Calculate(date, Longyearbyen);
            DailyPrayerTimes sistani = new PrayerTimeCalculator(
                CalculationMethod.MuslimWorldLeague, shiaMarja: ShiaMarja.Sistani).Calculate(date, Longyearbyen);
            DailyPrayerTimes makarem = new PrayerTimeCalculator(
                CalculationMethod.MuslimWorldLeague, shiaMarja: ShiaMarja.Makarem).Calculate(date, Longyearbyen);

            AssertEqualTimes(khamenei, sistani);
            AssertEqualTimes(khamenei, makarem);
        }

        // ----- 14. CalculationParameters with ShiaMarja -----

        [Test]
        public void ForMethod_TehranWithSistani_KeepsMarjaAndMarksShiaMethod()
        {
            CalculationParameters tehran = CalculationParameters.ForMethod(
                CalculationMethod.Tehran, shiaMarja: ShiaMarja.Sistani);
            CalculationParameters mwl = CalculationParameters.ForMethod(CalculationMethod.MuslimWorldLeague);

            Assert.AreEqual(ShiaMarja.Sistani, tehran.ShiaMarja);
            Assert.IsTrue(tehran.IsShiaMethod);
            Assert.IsFalse(mwl.IsShiaMethod);
        }

        [Test]
        public void ForMethod_ParametersDifferingOnlyInShiaMarja_AreNotEqualAndDefaultRuleIsAngleBased()
        {
            CalculationParameters sistani = CalculationParameters.ForMethod(
                CalculationMethod.Tehran, shiaMarja: ShiaMarja.Sistani);
            CalculationParameters makarem = CalculationParameters.ForMethod(
                CalculationMethod.Tehran, shiaMarja: ShiaMarja.Makarem);

            Assert.IsFalse(sistani.Equals(makarem));
            Assert.AreEqual(HighLatitudeRule.AngleBased,
                CalculationParameters.ForMethod(CalculationMethod.Tehran).HighLatitudeRule);
        }

        // ----- Helpers -----

        /// <summary>Fajr &lt; Sunrise &lt; Dhuhr &lt; Asr &lt; Maghrib &lt;= Isha.</summary>
        private static void AssertInOrder(DailyPrayerTimes times)
        {
            Assert.Less(times.Fajr, times.Sunrise, "Fajr < Sunrise");
            Assert.Less(times.Sunrise, times.Dhuhr, "Sunrise < Dhuhr");
            Assert.Less(times.Dhuhr, times.Asr, "Dhuhr < Asr");
            Assert.Less(times.Asr, times.Maghrib, "Asr < Maghrib");
            Assert.LessOrEqual(times.Maghrib, times.Isha, "Maghrib <= Isha");
        }

        private static void AssertEqualTimes(DailyPrayerTimes left, DailyPrayerTimes right)
        {
            Assert.AreEqual(left.Fajr, right.Fajr, "Fajr");
            Assert.AreEqual(left.Sunrise, right.Sunrise, "Sunrise");
            Assert.AreEqual(left.Dhuhr, right.Dhuhr, "Dhuhr");
            Assert.AreEqual(left.Asr, right.Asr, "Asr");
            Assert.AreEqual(left.Maghrib, right.Maghrib, "Maghrib");
            Assert.AreEqual(left.Isha, right.Isha, "Isha");
        }

        private static bool AllSixTimesEqual(DailyPrayerTimes left, DailyPrayerTimes right)
        {
            return left.Fajr == right.Fajr && left.Sunrise == right.Sunrise && left.Dhuhr == right.Dhuhr &&
                   left.Asr == right.Asr && left.Maghrib == right.Maghrib && left.Isha == right.Isha;
        }
    }
}
