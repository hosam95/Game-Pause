namespace GamePause.PrayerTimeCalculator.Enums
{
    /// <summary>
    /// Rules for adjusting prayer times at high latitudes
    /// where twilight may persist throughout the night.
    /// </summary>
    public enum HighLatitudeRule
    {
        /// <summary>
        /// No adjustment. May result in invalid times at extreme latitudes.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Fajr and Isha are placed at fractions of the night.
        /// Night is divided from sunset to sunrise.
        /// </summary>
        MiddleOfNight = 1,
        
        /// <summary>
        /// Isha at 1/7th of night, Fajr at 6/7th of night.
        /// </summary>
        SeventhOfNight = 2,
        
        /// <summary>
        /// Uses the angle that the sun would reach.
        /// Interpolates based on available twilight.
        /// </summary>
        AngleBased = 3
    }
}