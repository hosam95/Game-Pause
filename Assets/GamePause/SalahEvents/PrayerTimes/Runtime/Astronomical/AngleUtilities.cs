using System;

namespace GamePause.SalahEvents.PrayerTimes.Astronomical
{
    /// <summary>
    /// Utility methods for angle conversions and normalization.
    /// </summary>
    internal static class AngleUtilities
    {
        private const double DegToRadFactor = Math.PI / 180.0;
        private const double RadToDegFactor = 180.0 / Math.PI;

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        public static double DegreesToRadians(double degrees)
        {
            return degrees * DegToRadFactor;
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        public static double RadiansToDegrees(double radians)
        {
            return radians * RadToDegFactor;
        }

        /// <summary>
        /// Normalizes an angle to the range [0, 360).
        /// </summary>
        public static double NormalizeDegrees(double degrees)
        {
            double result = degrees % 360.0;
            return result < 0 ? result + 360.0 : result;
        }

        /// <summary>
        /// Sine of an angle in degrees.
        /// </summary>
        public static double SinDegrees(double degrees)
        {
            return Math.Sin(DegreesToRadians(degrees));
        }

        /// <summary>
        /// Cosine of an angle in degrees.
        /// </summary>
        public static double CosDegrees(double degrees)
        {
            return Math.Cos(DegreesToRadians(degrees));
        }

        /// <summary>
        /// Tangent of an angle in degrees.
        /// </summary>
        public static double TanDegrees(double degrees)
        {
            return Math.Tan(DegreesToRadians(degrees));
        }

        /// <summary>
        /// Arcsine returning degrees.
        /// </summary>
        public static double AsinDegrees(double value)
        {
            return RadiansToDegrees(Math.Asin(value));
        }

        /// <summary>
        /// Arccosine returning degrees.
        /// </summary>
        public static double AcosDegrees(double value)
        {
            return RadiansToDegrees(Math.Acos(value));
        }

        /// <summary>
        /// Arctangent returning degrees.
        /// </summary>
        public static double AtanDegrees(double value)
        {
            return RadiansToDegrees(Math.Atan(value));
        }

        /// <summary>
        /// Two-argument arctangent returning degrees.
        /// </summary>
        public static double Atan2Degrees(double y, double x)
        {
            return RadiansToDegrees(Math.Atan2(y, x));
        }
    }
}