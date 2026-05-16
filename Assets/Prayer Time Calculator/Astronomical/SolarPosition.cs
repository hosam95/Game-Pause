namespace GamePause.PrayerTimeCalculator.Astronomical
{
    /// <summary>
    /// Contains solar position data for a specific moment.
    /// </summary>
    public readonly struct SolarPosition
    {
        /// <summary>
        /// Solar declination in degrees.
        /// The angle between the sun and the celestial equator.
        /// </summary>
        public double Declination { get; }

        /// <summary>
        /// Equation of time in minutes.
        /// Difference between apparent solar time and mean solar time.
        /// </summary>
        public double EquationOfTime { get; }

        public SolarPosition(double declination, double equationOfTime)
        {
            Declination = declination;
            EquationOfTime = equationOfTime;
        }
    }
}