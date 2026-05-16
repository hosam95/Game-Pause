namespace GamePause.PrayerTimeCalculator.Enums
{
    /// <summary>
    /// Juristic methods for calculating Asr prayer time.
    /// </summary>
    public enum AsrJuristicMethod
    {
        /// <summary>
        /// Shadow equals object length + noon shadow.
        /// Used by Shafi'i, Maliki, and Hanbali schools.
        /// </summary>
        Standard = 1,
        
        /// <summary>
        /// Shadow equals twice object length + noon shadow.
        /// Used by Hanafi school.
        /// </summary>
        Hanafi = 2
    }
}