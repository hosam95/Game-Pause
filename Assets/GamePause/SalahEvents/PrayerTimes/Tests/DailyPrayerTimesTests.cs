using System;
using NUnit.Framework;
using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

namespace GamePause.SalahEvents.PrayerTimes.Tests
{
    [TestFixture]
    public class DailyPrayerTimesTests
    {
        private static readonly DateTime Date = new DateTime(2026, 10, 9);
        private static readonly GeographicCoordinates Coordinates = new GeographicCoordinates(30.0444, 31.2357);
        private static readonly TimeZoneInfo PlusThree =
            TimeZoneInfo.CreateCustomTimeZone("Test+3", TimeSpan.FromHours(3), "Test+3", "Test+3");

        // ----- Get -----

        [Test]
        public void Get_AllPrayerTypes_ReturnsMatchingProperty()
        {
            DailyPrayerTimes times = CreateComplete();

            Assert.AreEqual(times.Fajr, times.Get(PrayerType.Fajr));
            Assert.AreEqual(times.Sunrise, times.Get(PrayerType.Sunrise));
            Assert.AreEqual(times.Dhuhr, times.Get(PrayerType.Dhuhr));
            Assert.AreEqual(times.Asr, times.Get(PrayerType.Asr));
            Assert.AreEqual(times.Maghrib, times.Get(PrayerType.Maghrib));
            Assert.AreEqual(times.Isha, times.Get(PrayerType.Isha));
        }

        // ----- IsEstimated / HasEstimatedTimes -----

        [Test]
        public void IsEstimated_FlagsRoundTripForAllSixPrayers()
        {
            bool[] estimated = { true, false, true, false, true, false };
            DailyPrayerTimes times = Create(Utc(2, 26), Utc(3, 52), Utc(9, 42), Utc(13, 2), Utc(15, 31), Utc(16, 49), estimated);

            Assert.IsTrue(times.IsEstimated(PrayerType.Fajr));
            Assert.IsFalse(times.IsEstimated(PrayerType.Sunrise));
            Assert.IsTrue(times.IsEstimated(PrayerType.Dhuhr));
            Assert.IsFalse(times.IsEstimated(PrayerType.Asr));
            Assert.IsTrue(times.IsEstimated(PrayerType.Maghrib));
            Assert.IsFalse(times.IsEstimated(PrayerType.Isha));
        }

        [Test]
        public void HasEstimatedTimes_TrueIffAnyFlagIsSet_FalseWhenArgumentOmitted()
        {
            Assert.IsFalse(CreateComplete().HasEstimatedTimes, "Omitting the estimated argument should mean no estimated times");

            DailyPrayerTimes allFalse = Create(Utc(2, 26), Utc(3, 52), Utc(9, 42), Utc(13, 2), Utc(15, 31), Utc(16, 49), new bool[6]);
            Assert.IsFalse(allFalse.HasEstimatedTimes, "All-false flags should mean no estimated times");

            DailyPrayerTimes fajrOnly = Create(Utc(2, 26), Utc(3, 52), Utc(9, 42), Utc(13, 2), Utc(15, 31), Utc(16, 49),
                new[] { true, false, false, false, false, false });
            Assert.IsTrue(fajrOnly.HasEstimatedTimes, "A single true flag should make HasEstimatedTimes true");

            DailyPrayerTimes ishaOnly = Create(Utc(2, 26), Utc(3, 52), Utc(9, 42), Utc(13, 2), Utc(15, 31), Utc(16, 49),
                new[] { false, false, false, false, false, true });
            Assert.IsTrue(ishaOnly.HasEstimatedTimes, "A single true flag should make HasEstimatedTimes true");
        }

        [Test]
        public void Constructor_CopiesEstimatedArray_ChangingCallerArrayDoesNotChangeResult()
        {
            bool[] estimated = new bool[6];
            DailyPrayerTimes times = Create(Utc(2, 26), Utc(3, 52), Utc(9, 42), Utc(13, 2), Utc(15, 31), Utc(16, 49), estimated);

            estimated[0] = true;

            Assert.IsFalse(times.IsEstimated(PrayerType.Fajr), "The constructor must copy the array; caller changes must not leak in");
        }

        [Test]
        public void Constructor_EstimatedArrayWrongLength_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                Create(Utc(2, 26), Utc(3, 52), Utc(9, 42), Utc(13, 2), Utc(15, 31), Utc(16, 49), new[] { true }));
        }

        // ----- TryGetNext -----

        [Test]
        public void TryGetNext_BeforeFajr_ReturnsFajr()
        {
            DailyPrayerTimes times = CreateComplete();

            Assert.IsTrue(times.TryGetNext(Utc(2, 0), out PrayerType prayer, out DateTime time));
            Assert.AreEqual(PrayerType.Fajr, prayer);
            Assert.AreEqual(Utc(2, 26), time);
        }

        [Test]
        public void TryGetNext_BetweenFajrAndSunrise_ReturnsSunrise()
        {
            DailyPrayerTimes times = CreateComplete();

            Assert.IsTrue(times.TryGetNext(Utc(3, 0), out PrayerType prayer, out DateTime time));
            Assert.AreEqual(PrayerType.Sunrise, prayer);
            Assert.AreEqual(Utc(3, 52), time);
        }

        [Test]
        public void TryGetNext_ExactlyAtFajr_ReturnsSunriseBecauseComparisonIsStrictlyAfter()
        {
            DailyPrayerTimes times = CreateComplete();

            Assert.IsTrue(times.TryGetNext(Utc(2, 26), out PrayerType prayer, out _));
            Assert.AreEqual(PrayerType.Sunrise, prayer);
        }

        [Test]
        public void TryGetNext_AfterIsha_ReturnsFalse()
        {
            DailyPrayerTimes times = CreateComplete();

            Assert.IsFalse(times.TryGetNext(Utc(17, 0), out PrayerType prayer, out DateTime time));
            Assert.AreEqual(default(PrayerType), prayer);
            Assert.AreEqual(default(DateTime), time);
        }

        [Test]
        public void TryGetNext_ExactlyAtIsha_ReturnsFalseBecauseComparisonIsStrictlyAfter()
        {
            DailyPrayerTimes times = CreateComplete();

            Assert.IsFalse(times.TryGetNext(Utc(16, 49), out _, out _));
        }

        // ----- GetLocal -----

        [Test]
        public void GetLocal_PlusThreeTimeZone_ConvertsToThatZone()
        {
            DailyPrayerTimes times = CreateComplete();

            DateTime local = times.GetLocal(PrayerType.Fajr, PlusThree);

            Assert.AreEqual(new DateTime(2026, 10, 9, 5, 26, 0), local);
        }

        [Test]
        public void GetLocal_NullTimeZone_ThrowsArgumentNullException()
        {
            DailyPrayerTimes times = CreateComplete();

            Assert.Throws<ArgumentNullException>(() => times.GetLocal(PrayerType.Dhuhr, null));
        }

        // ----- Helpers -----

        private static DailyPrayerTimes CreateComplete()
        {
            return Create(Utc(2, 26), Utc(3, 52), Utc(9, 42), Utc(13, 2), Utc(15, 31), Utc(16, 49));
        }

        private static DailyPrayerTimes Create(
            DateTime fajr, DateTime sunrise, DateTime dhuhr, DateTime asr, DateTime maghrib, DateTime isha,
            bool[] estimated = null)
        {
            return new DailyPrayerTimes(Date, Coordinates, fajr, sunrise, dhuhr, asr, maghrib, isha, estimated);
        }

        private static DateTime Utc(int hour, int minute)
        {
            return new DateTime(2026, 10, 9, hour, minute, 0, DateTimeKind.Utc);
        }
    }
}
