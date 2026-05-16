using System;

namespace GamePause.PrayerTimeCalculator.Astronomical
{
    /// <summary>
    /// Converts between DateTime and Julian Day numbers.
    /// Based on NOAA Solar Calculator algorithms.
    /// </summary>
    public static class JulianDateCalculator
    {
        /// <summary>
        /// Converts a UTC DateTime to Julian Day number.
        /// </summary>
        /// <param name="dateTime">UTC DateTime to convert.</param>
        /// <returns>Julian Day number.</returns>
        public static double ToJulianDay(DateTime dateTime)
        {
            int year = dateTime.Year;
            int month = dateTime.Month;
            double day = dateTime.Day + 
                         dateTime.Hour / 24.0 + 
                         dateTime.Minute / 1440.0 + 
                         dateTime.Second / 86400.0;

            // Adjust for January and February
            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }

            int a = year / 100;
            int b = 2 - a + (a / 4);

            double julianDay = Math.Floor(365.25 * (year + 4716)) +
                               Math.Floor(30.6001 * (month + 1)) +
                               day + b - 1524.5;

            return julianDay;
        }

        /// <summary>
        /// Converts a Julian Day number to UTC DateTime.
        /// </summary>
        /// <param name="julianDay">Julian Day number to convert.</param>
        /// <returns>UTC DateTime.</returns>
        public static DateTime FromJulianDay(double julianDay)
        {
            double z = Math.Floor(julianDay + 0.5);
            double f = julianDay + 0.5 - z;

            double a;
            if (z < 2299161)
            {
                a = z;
            }
            else
            {
                double alpha = Math.Floor((z - 1867216.25) / 36524.25);
                a = z + 1 + alpha - Math.Floor(alpha / 4);
            }

            double b = a + 1524;
            double c = Math.Floor((b - 122.1) / 365.25);
            double d = Math.Floor(365.25 * c);
            double e = Math.Floor((b - d) / 30.6001);

            double dayWithFraction = b - d - Math.Floor(30.6001 * e) + f;
            int day = (int)Math.Floor(dayWithFraction);
            
            int month = (e < 14) ? (int)e - 1 : (int)e - 13;
            int year = (month > 2) ? (int)c - 4716 : (int)c - 4715;

            double fractionOfDay = dayWithFraction - day;
            int hours = (int)Math.Floor(fractionOfDay * 24);
            int minutes = (int)Math.Floor((fractionOfDay * 24 - hours) * 60);
            int seconds = (int)Math.Round(((fractionOfDay * 24 - hours) * 60 - minutes) * 60);

            // Handle second overflow
            if (seconds >= 60)
            {
                seconds = 0;
                minutes++;
            }
            if (minutes >= 60)
            {
                minutes = 0;
                hours++;
            }

            return new DateTime(year, month, day, hours, minutes, seconds, DateTimeKind.Utc);
        }

        /// <summary>
        /// Gets Julian Day for the start of a given date (00:00:00 UTC).
        /// </summary>
        public static double ToJulianDayStartOfDay(DateTime date)
        {
            return ToJulianDay(new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <summary>
        /// Calculates Julian Century from Julian Day.
        /// Used in many solar calculations.
        /// </summary>
        /// <param name="julianDay">Julian Day number.</param>
        /// <returns>Julian Century (T).</returns>
        public static double ToJulianCentury(double julianDay)
        {
            return (julianDay - 2451545.0) / 36525.0;
        }
    }
}