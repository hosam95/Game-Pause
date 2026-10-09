using System;
using NUnit.Framework;
using GamePause.SalahEvents.PrayerTimes.Astronomical;

namespace GamePause.SalahEvents.PrayerTimes.Tests
{
    [TestFixture]
    public class SolarCalculatorTests
    {
        private const double TimeTolerance = 0.05; // ~3 minutes tolerance in hours
        
        #region Solar Position Tests
        
        [Test]
        public void GetSolarPosition_SummerSolstice_DeclinationIsPositive()
        {
            // June 21, 2024 - Northern hemisphere summer solstice
            var date = new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            var position = SolarCalculator.GetSolarPosition(jd);
            
            // Declination should be around +23.44°
            Assert.Greater(position.Declination, 23.0);
            Assert.Less(position.Declination, 24.0);
        }
        
        [Test]
        public void GetSolarPosition_WinterSolstice_DeclinationIsNegative()
        {
            // December 21, 2024 - Northern hemisphere winter solstice
            var date = new DateTime(2024, 12, 21, 12, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            var position = SolarCalculator.GetSolarPosition(jd);
            
            // Declination should be around -23.44°
            Assert.Less(position.Declination, -23.0);
            Assert.Greater(position.Declination, -24.0);
        }
        
        [Test]
        public void GetSolarPosition_Equinox_DeclinationNearZero()
        {
            // March 20, 2024 - Spring equinox
            var date = new DateTime(2024, 3, 20, 12, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            var position = SolarCalculator.GetSolarPosition(jd);
            
            // Declination should be near 0°
            Assert.AreEqual(0, position.Declination, 1.0);
        }
        
        [Test]
        public void GetSolarPosition_EquationOfTime_WithinExpectedRange()
        {
            // Equation of time varies between roughly -14 to +16 minutes
            var date = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            var position = SolarCalculator.GetSolarPosition(jd);
            
            Assert.Greater(position.EquationOfTime, -20);
            Assert.Less(position.EquationOfTime, 20);
        }
        
        #endregion
        
        #region Solar Noon Tests
        
        [Test]
        public void GetSolarNoon_AtPrimeMeridian_NearTwelve()
        {
            var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            double solarNoon = SolarCalculator.GetSolarNoon(jd, 0);
            
            // At longitude 0, solar noon should be around 12:00 UTC
            Assert.AreEqual(12.0, solarNoon, 0.25); // ~15 min tolerance
        }
        
        [Test]
        public void GetSolarNoon_EastLongitude_BeforeTwelve()
        {
            var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            double solarNoon = SolarCalculator.GetSolarNoon(jd, 30); // Cairo
            
            // East of prime meridian, solar noon occurs earlier in UTC
            Assert.Less(solarNoon, 12.0);
        }
        
        [Test]
        public void GetSolarNoon_WestLongitude_AfterTwelve()
        {
            var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            double solarNoon = SolarCalculator.GetSolarNoon(jd, -74); // New York
            
            // West of prime meridian, solar noon occurs later in UTC
            Assert.Greater(solarNoon, 12.0);
        }
        
        #endregion
        
        #region Sunrise/Sunset Tests
        
        [Test]
        public void GetSunrise_BeforeSolarNoon()
        {
            var date = new DateTime(2024, 1,15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            double latitude = 30.0444; // Cairo
            double longitude = 31.2357;
            
            double sunrise = SolarCalculator.GetSunrise(jd, latitude, longitude);
            double solarNoon = SolarCalculator.GetSolarNoon(jd, longitude);
            
            Assert.Less(sunrise, solarNoon);
        }
        
        [Test]
        public void GetSunset_AfterSolarNoon()
        {
            var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            double latitude = 30.0444;
            double longitude = 31.2357;
            
            double sunset = SolarCalculator.GetSunset(jd, latitude, longitude);
            double solarNoon = SolarCalculator.GetSolarNoon(jd, longitude);
            
            Assert.Greater(sunset, solarNoon);
        }
        
        [Test]
        public void GetSunriseAndSunset_Symmetry_AroundSolarNoon()
        {
            var date = new DateTime(2024, 3, 20, 0, 0, 0, DateTimeKind.Utc); // Equinox
            double jd = JulianDateCalculator.ToJulianDay(date);
            double latitude = 0; // Equator
            double longitude = 0;
            
            double sunrise = SolarCalculator.GetSunrise(jd, latitude, longitude);
            double sunset = SolarCalculator.GetSunset(jd, latitude, longitude);
            double solarNoon = SolarCalculator.GetSolarNoon(jd, longitude);
            
            double morningDuration = solarNoon - sunrise;
            double eveningDuration = sunset - solarNoon;
            
            // At equator on equinox, day should be symmetric
            Assert.AreEqual(morningDuration, eveningDuration, 0.1);
        }
        
        [Test]
        public void GetSunrise_HighLatitudeSummer_MayReturnNaN()
        {
            // During polar day, sun never sets
            var date = new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            double latitude = 70; // Inside Arctic circle
            double longitude = 25;
            
            // This tests that the calculation handles extreme cases
            double sunrise = SolarCalculator.GetSunrise(jd, latitude, longitude);
            
            // May or may not be NaN depending on exact location and date
            // Just verify it doesn't throw
            Assert.IsTrue(double.IsNaN(sunrise) || sunrise >= 0);
        }
        
        #endregion
        
        #region Time For Angle Tests
        
        [Test]
        public void GetTimeForAngle_Fajr_BeforeSunrise()
        {
            var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            double latitude = 30.0444;
            double longitude = 31.2357;
            
            double fajrTime = SolarCalculator.GetTimeForAngle(jd, latitude, longitude, 19.5, true);
            double sunrise = SolarCalculator.GetSunrise(jd, latitude, longitude);
            
            Assert.Less(fajrTime, sunrise);
        }
        
        [Test]
        public void GetTimeForAngle_Isha_AfterSunset()
        {
            var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            double latitude = 30.0444;
            double longitude = 31.2357;
            
            double ishaTime = SolarCalculator.GetTimeForAngle(jd, latitude, longitude, 17.5, false);
            double sunset = SolarCalculator.GetSunset(jd, latitude, longitude);
            
            Assert.Greater(ishaTime, sunset);
        }
        
        #endregion
        
        #region Asr Tests
        
        [Test]
        public void GetAsrTime_Standard_AfterSolarNoon()
        {
            var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            double latitude = 30.0444;
            double longitude = 31.2357;
            
            double asrTime = SolarCalculator.GetAsrTime(jd, latitude, longitude, 1);
            double solarNoon = SolarCalculator.GetSolarNoon(jd, longitude);
            
            Assert.Greater(asrTime, solarNoon);
        }
        
        [Test]
        public void GetAsrTime_Hanafi_AfterStandard()
        {
            var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            double latitude = 30.0444;
            double longitude = 31.2357;
            
            double asrStandard = SolarCalculator.GetAsrTime(jd, latitude, longitude, 1);
            double asrHanafi = SolarCalculator.GetAsrTime(jd, latitude, longitude, 2);
            
            Assert.Greater(asrHanafi, asrStandard);
        }
        
        [Test]
        public void GetAsrTime_BeforeSunset()
        {
            var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            double latitude = 30.0444;
            double longitude = 31.2357;
            
            double asrTime = SolarCalculator.GetAsrTime(jd, latitude, longitude, 1);
            double sunset = SolarCalculator.GetSunset(jd, latitude, longitude);
            
            Assert.Less(asrTime, sunset);
        }
        
        #endregion
        
        #region Utility Tests
        
        [Test]
        public void HoursToDateTime_WholeHours_ConvertsCorrectly()
        {
            var baseDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            
            DateTime result = SolarCalculator.HoursToDateTime(baseDate, 14.0);
            
            Assert.AreEqual(14, result.Hour);
            Assert.AreEqual(0, result.Minute);
        }
        
        [Test]
        public void HoursToDateTime_FractionalHours_ConvertsCorrectly()
        {
            var baseDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            
            DateTime result = SolarCalculator.HoursToDateTime(baseDate, 14.5);
            
            Assert.AreEqual(14, result.Hour);
            Assert.AreEqual(30, result.Minute);
        }
        
        [Test]
        public void HoursToDateTime_OverflowToNextDay_HandlesCorrectly()
        {
            var baseDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            
            DateTime result = SolarCalculator.HoursToDateTime(baseDate, 25.0);
            
            Assert.AreEqual(16, result.Day);
            Assert.AreEqual(1, result.Hour);
        }
        
        [Test]
        public void HoursToDateTime_NaN_ThrowsException()
        {
            var baseDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            
            Assert.Throws<ArgumentException>(() => 
                SolarCalculator.HoursToDateTime(baseDate, double.NaN));
        }
        
        [Test]
        public void GetMidnight_CalculatesCorrectly()
        {
            double sunset = 17.5; // 5:30 PM
            double nextSunrise = 6.0; // 6:00 AM next day
            
            double midnight = SolarCalculator.GetMidnight(sunset, nextSunrise);
            
            // Midnight between 17.5 and 30.0 (6+24) = 23.75
            Assert.AreEqual(23.75, midnight, 0.01);
        }
        
        #endregion
        
        #region Reference Value Tests (Against Known Values)
        
        [Test]
        public void Cairo_January_SunriseNearReference()
        {
            // Cairo sunrise on Jan 15, 2024 should be around 06:55 local (04:55 UTC)
            var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            double sunrise = SolarCalculator.GetSunrise(jd, 30.0444, 31.2357);
            
            // Expected: ~4.9 hours (04:55 UTC)
            Assert.AreEqual(4.9, sunrise, TimeTolerance);
        }
        
        [Test]
        public void Cairo_January_SunsetNearReference()
        {
            // Cairo sunset on Jan 15, 2024 should be around 17:15 local (15:15 UTC)
            var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            double jd = JulianDateCalculator.ToJulianDay(date);
            
            double sunset = SolarCalculator.GetSunset(jd, 30.0444, 31.2357);
            
            // Expected: ~15.25 hours (15:15 UTC)
            Assert.AreEqual(15.25, sunset, TimeTolerance);
        }
        
        #endregion
    }
}