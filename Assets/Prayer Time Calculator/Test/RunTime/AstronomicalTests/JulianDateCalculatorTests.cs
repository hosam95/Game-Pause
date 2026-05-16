using System;
using NUnit.Framework;
using GamePause.PrayerTimeCalculator.Astronomical;

namespace GamePause.PrayerTimeCalculator.Tests
{
    [TestFixture]
    public class JulianDateCalculatorTests
    {
        // Reference: J2000.0 epoch is January 1, 2000, 12:00 TT = JD 2451545.0
        
        [Test]
        public void ToJulianDay_J2000Epoch_ReturnsCorrectValue()
        {
            var date = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            Assert.AreEqual(2451545.0, jd, 0.0001);
        }
        
        [Test]
        public void ToJulianDay_KnownDate_ReturnsCorrectValue()
        {
            // Reference: January 1, 2024, 00:00 UTC = JD 2460310.5
            var date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            Assert.AreEqual(2460310.5, jd, 0.0001);
        }
        
        [Test]
        public void ToJulianDay_WithTime_IncludesTimeFraction()
        {
            var date = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            // Noon adds 0.5 to the Julian Day
            Assert.AreEqual(2460311.0, jd, 0.0001);
        }
        
        [Test]
        public void FromJulianDay_J2000Epoch_ReturnsCorrectDate()
        {
            double jd = 2451545.0;
            
            DateTime date = JulianDateCalculator.FromJulianDay(jd);
            
            Assert.AreEqual(2000, date.Year);
            Assert.AreEqual(1, date.Month);
            Assert.AreEqual(1, date.Day);
            Assert.AreEqual(12, date.Hour);
        }
        
        [Test]
        public void RoundTrip_PreservesDateTime()
        {
            var original = new DateTime(2024, 6, 15, 14, 30, 45, DateTimeKind.Utc);
            
            double jd = JulianDateCalculator.ToJulianDay(original);
            DateTime result = JulianDateCalculator.FromJulianDay(jd);
            
            Assert.AreEqual(original.Year, result.Year);
            Assert.AreEqual(original.Month, result.Month);
            Assert.AreEqual(original.Day, result.Day);
            Assert.AreEqual(original.Hour, result.Hour);
            Assert.AreEqual(original.Minute, result.Minute);
            Assert.AreEqual(original.Second, result.Second, 1); // Allow 1 second tolerance
        }
        
        [Test]
        public void ToJulianCentury_J2000_ReturnsZero()
        {
            double jd = 2451545.0;
            
            double T = JulianDateCalculator.ToJulianCentury(jd);
            
            Assert.AreEqual(0, T, 0.0001);
        }
        
        [Test]
        public void ToJulianCentury_2100_ReturnsOne()
        {
            // 100 years after J2000 = 1 Julian Century
            double jd = 2451545.0 + (100 * 365.25);
            
            double T = JulianDateCalculator.ToJulianCentury(jd);
            
            Assert.AreEqual(1.0, T, 0.01);
        }
        
        [Test]
        public void ToJulianDayStartOfDay_IgnoresTime()
        {
            var date1 = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            var date2 = new DateTime(2024, 6, 15, 23, 59, 59, DateTimeKind.Utc);
            
            double jd1 = JulianDateCalculator.ToJulianDayStartOfDay(date1);
            double jd2 = JulianDateCalculator.ToJulianDayStartOfDay(date2);
            
            Assert.AreEqual(jd1, jd2);
        }
    }
}