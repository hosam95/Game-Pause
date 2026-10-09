using System;

namespace GamePause.SalahEvents.PrayerTimes.Astronomical
{
    /// <summary>
    /// Calculates solar position and related times.
    /// Based on NOAA Solar Calculator and PrayTimes.org algorithms.
    /// </summary>
    public static class SolarCalculator
    {
        #region Solar Position

        /// <summary>
        /// Calculates solar position for a given Julian Day.
        /// </summary>
        public static SolarPosition GetSolarPosition(double julianDay)
        {
            double T = JulianDateCalculator.ToJulianCentury(julianDay);

            double declination = CalculateSunDeclination(T);
            double equationOfTime = CalculateEquationOfTime(T);

            return new SolarPosition(declination, equationOfTime);
        }

        /// <summary>
        /// Calculates the sun's declination for a Julian Century.
        /// </summary>
        private static double CalculateSunDeclination(double T)
        {
            double obliquityCorrection = CalculateObliquityCorrection(T);
            double apparentLongitude = CalculateSunApparentLongitude(T);

            double sinDeclination = AngleUtilities.SinDegrees(obliquityCorrection) *
                                    AngleUtilities.SinDegrees(apparentLongitude);

            return AngleUtilities.AsinDegrees(sinDeclination);
        }

        /// <summary>
        /// Calculates the equation of time in minutes.
        /// </summary>
        private static double CalculateEquationOfTime(double T)
        {
            double obliquityCorrection = CalculateObliquityCorrection(T);
            double geomMeanLongSun = CalculateGeomMeanLongSun(T);
            double eccentricityEarthOrbit = CalculateEccentricityEarthOrbit(T);
            double geomMeanAnomalySun = CalculateGeomMeanAnomalySun(T);

            double y = Math.Tan(AngleUtilities.DegreesToRadians(obliquityCorrection / 2));
            y *= y;

            double sin2L0 = AngleUtilities.SinDegrees(2 * geomMeanLongSun);
            double cos2L0 = AngleUtilities.CosDegrees(2 * geomMeanLongSun);
            double sin4L0 = AngleUtilities.SinDegrees(4 * geomMeanLongSun);
            double sinM = AngleUtilities.SinDegrees(geomMeanAnomalySun);
            double sin2M = AngleUtilities.SinDegrees(2 * geomMeanAnomalySun);

            double equationOfTime = y * sin2L0
                                  - 2 * eccentricityEarthOrbit * sinM
                                  + 4 * eccentricityEarthOrbit * y * sinM * cos2L0
                                  - 0.5 * y * y * sin4L0
                                  - 1.25 * eccentricityEarthOrbit * eccentricityEarthOrbit * sin2M;

            return AngleUtilities.RadiansToDegrees(equationOfTime) * 4; // Convert to minutes
        }

        #endregion

        #region Time Calculations

        /// <summary>
        /// Calculates solar noon time in hours (UTC) for a given date and longitude.
        /// </summary>
        public static double GetSolarNoon(double julianDay, double longitude)
        {
            var solarPosition = GetSolarPosition(julianDay);
            return 12.0 - (longitude / 15.0) - (solarPosition.EquationOfTime / 60.0);
        }

        /// <summary>
        /// Calculates the time for a given sun angle below/above horizon.
        /// </summary>
        /// <param name="julianDay">Julian Day for the calculation date.</param>
        /// <param name="latitude">Observer's latitude in degrees.</param>
        /// <param name="longitude">Observer's longitude in degrees.</param>
        /// <param name="angle">Sun angle in degrees. Positive = below horizon.</param>
        /// <param name="direction">True for rising (morning), false for setting (evening).</param>
        /// <returns>Time in hours (UTC), or NaN if sun never reaches angle.</returns>
        public static double GetTimeForAngle(
            double julianDay,
            double latitude,
            double longitude,
            double angle,
            bool direction)
        {
            double solarNoon = GetSolarNoon(julianDay, longitude);
            var solarPosition = GetSolarPosition(julianDay);

            double hourAngle = CalculateHourAngle(latitude, solarPosition.Declination, angle);

            if (double.IsNaN(hourAngle))
            {
                return double.NaN;
            }

            // Convert hour angle to time offset
            double timeOffset = hourAngle / 15.0;

            return direction
                ? solarNoon - timeOffset  // Rising (before noon)
                : solarNoon + timeOffset; // Setting (after noon)
        }

        /// <summary>
        /// Calculates sunrise time in hours (UTC).
        /// </summary>
        public static double GetSunrise(double julianDay, double latitude, double longitude)
        {
            return GetTimeForAngle(julianDay, latitude, longitude, 0.833, true);
        }

        /// <summary>
        /// Calculates sunset time in hours (UTC).
        /// </summary>
        public static double GetSunset(double julianDay, double latitude, double longitude)
        {
            return GetTimeForAngle(julianDay, latitude, longitude, 0.833, false);
        }

        /// <summary>
        /// Calculates the hour angle for a given sun angle.
        /// </summary>
        /// <param name="latitude">Observer's latitude in degrees.</param>
        /// <param name="declination">Sun's declination in degrees.</param>
        /// <param name="angle">Desired sun angle in degrees.</param>
        /// <returns>Hour angle in degrees, or NaN if impossible.</returns>
        private static double CalculateHourAngle(double latitude, double declination, double angle)
        {
            double latRad = AngleUtilities.DegreesToRadians(latitude);
            double decRad = AngleUtilities.DegreesToRadians(declination);
            double angleRad = AngleUtilities.DegreesToRadians(angle);

            double cosHourAngle = (-Math.Sin(angleRad) - Math.Sin(latRad) * Math.Sin(decRad)) /
                                  (Math.Cos(latRad) * Math.Cos(decRad));

            // Check if sun never reaches this angle
            if (cosHourAngle < -1 || cosHourAngle > 1)
            {
                return double.NaN;
            }

            return AngleUtilities.AcosDegrees(cosHourAngle);
        }

        #endregion

        #region Asr Calculation

        /// <summary>
        /// Calculates Asr time using shadow length method.
        /// </summary>
        /// <param name="julianDay">Julian Day for the calculation date.</param>
        /// <param name="latitude">Observer's latitude in degrees.</param>
        /// <param name="longitude">Observer's longitude in degrees.</param>
        /// <param name="shadowFactor">Shadow factor (1 for Standard, 2 for Hanafi).</param>
        /// <returns>Asr time in hours (UTC), or NaN if the sun never rises.</returns>
        public static double GetAsrTime(
            double julianDay,
            double latitude,
            double longitude,
            int shadowFactor)
        {
            var solarPosition = GetSolarPosition(julianDay);
            double declination = solarPosition.Declination;

            // The sun stays below the horizon all day (polar night), so there is no shadow to measure.
            if (Math.Abs(latitude - declination) >= 90.0)
            {
                return double.NaN;
            }

            // Calculate the angle when shadow = shadowFactor * object height + noon shadow
            double angle = CalculateAsrAngle(latitude, declination, shadowFactor);

            return GetTimeForAngle(julianDay, latitude, longitude, -angle, false);
        }

        /// <summary>
        /// Calculates the sun angle for Asr based on shadow factor.
        /// </summary>
        private static double CalculateAsrAngle(double latitude, double declination, int shadowFactor)
        {
            double diff = Math.Abs(latitude - declination);
            double tanDiff = AngleUtilities.TanDegrees(diff);

            return AngleUtilities.AtanDegrees(1.0 / (shadowFactor + tanDiff));
        }

        #endregion

        #region Helper Calculations (NOAA Formulas)

        private static double CalculateGeomMeanLongSun(double T)
        {
            double L0 = 280.46646 + T * (36000.76983 + T * 0.0003032);
            return AngleUtilities.NormalizeDegrees(L0);
        }

        private static double CalculateGeomMeanAnomalySun(double T)
        {
            return 357.52911 + T * (35999.05029 - T * 0.0001537);
        }

        private static double CalculateEccentricityEarthOrbit(double T)
        {
            return 0.016708634 - T * (0.000042037 + T * 0.0000001267);
        }

        private static double CalculateSunEquationOfCenter(double T)
        {
            double M = CalculateGeomMeanAnomalySun(T);

            double sinM = AngleUtilities.SinDegrees(M);
            double sin2M = AngleUtilities.SinDegrees(2 * M);
            double sin3M = AngleUtilities.SinDegrees(3 * M);

            return sinM * (1.914602 - T * (0.004817 + T * 0.000014))
                 + sin2M * (0.019993 - T * 0.000101)
                 + sin3M * 0.000289;
        }

        private static double CalculateSunTrueLongitude(double T)
        {
            return CalculateGeomMeanLongSun(T) + CalculateSunEquationOfCenter(T);
        }

        private static double CalculateSunApparentLongitude(double T)
        {
            double trueLongitude = CalculateSunTrueLongitude(T);
            double omega = 125.04 - 1934.136 * T;
            return trueLongitude - 0.00569 - 0.00478 * AngleUtilities.SinDegrees(omega);
        }

        private static double CalculateMeanObliquityOfEcliptic(double T)
        {
            double seconds = 21.448 - T * (46.8150 + T * (0.00059 - T * 0.001813));
            return 23.0 + (26.0 + seconds / 60.0) / 60.0;
        }

        private static double CalculateObliquityCorrection(double T)
        {
            double obliquity = CalculateMeanObliquityOfEcliptic(T);
            double omega = 125.04 - 1934.136 * T;
            return obliquity + 0.00256 * AngleUtilities.CosDegrees(omega);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Converts hours (decimal) to DateTime on a given date.
        /// </summary>
        /// <param name="baseDate">The date for the time.</param>
        /// <param name="hours">Hours as decimal (e.g., 14.5 = 2:30 PM).</param>
        /// <returns>UTC DateTime, or next day if hours >= 24.</returns>
        public static DateTime HoursToDateTime(DateTime baseDate, double hours)
        {
            if (double.IsNaN(hours))
            {
                throw new ArgumentException("Cannot convert NaN hours to DateTime.", nameof(hours));
            }

            // Handle overflow to next day
            int dayOffset = 0;
            while (hours >= 24)
            {
                hours -= 24;
                dayOffset++;
            }
            while (hours < 0)
            {
                hours += 24;
                dayOffset--;
            }

            int totalSeconds = (int)Math.Round(hours * 3600);
            int h = totalSeconds / 3600;
            int m = (totalSeconds % 3600) / 60;
            int s = totalSeconds % 60;

            return new DateTime(
                baseDate.Year, baseDate.Month, baseDate.Day,
                h, m, s, DateTimeKind.Utc
            ).AddDays(dayOffset);
        }

        /// <summary>
        /// Calculates midnight between sunset and next sunrise.
        /// </summary>
        public static double GetMidnight(double sunset, double nextSunrise)
        {
            if (nextSunrise < sunset)
            {
                nextSunrise += 24;
            }
            return (sunset + nextSunrise) / 2.0;
        }

        #endregion
    }
}