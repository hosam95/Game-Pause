namespace GamePause.SalahEvents.PrayerTimes.Enums
{
    /// <summary>
    /// Whose rulings the Shia methods (<see cref="CalculationMethod.Tehran"/>, <see cref="CalculationMethod.Qom"/>)
    /// follow at high latitudes. For these methods Maghrib is always the local astronomical time
    /// (never shortened by <see cref="HighLatitudeRule"/>), and polar days use the marjaʿ's reference
    /// instead of <see cref="PolarEstimationRule"/>.
    /// Where twilight never ends but the sun rises and sets, none of the rulings found address
    /// Fajr / Isha, so the general <see cref="HighLatitudeRule"/> is used (provisional).
    /// </summary>
    public enum ShiaMarja
    {
        /// <summary>
        /// Follow the local horizon (Ajwibat al-Istiftaʾāt, q. 357).
        /// The ruling does not address polar day or night; 45° on the same meridian is used there (provisional).
        /// </summary>
        Khamenei = 0,

        /// <summary>
        /// Obligatory precaution: follow the nearest place that has a day and night within 24 hours
        /// (Minhaj al-Ṣāliḥīn 1/469, m. 88). Implemented as the nearest such latitude on the same meridian.
        /// </summary>
        Sistani = 1,

        /// <summary>
        /// Follow the day and night lengths of temperate regions on the same meridian
        /// (Istiftāʾāt Jadīd 2/73, q. 139; after al-ʿUrwa al-Wuthqā). "Temperate" is taken as 45° (provisional).
        /// </summary>
        Makarem = 2
    }
}
