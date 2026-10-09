namespace GamePause.SalahEvents.PrayerTimes.Enums
{
    /// <summary>
    /// How prayer times are estimated when the sun never reaches a required angle
    /// (polar day or polar night, or twilight that never ends with <see cref="HighLatitudeRule.None"/>).
    /// Every rule borrows the shape of the day from a reference latitude on the same longitude,
    /// so Dhuhr stays at local solar noon; only the reference latitude differs.
    /// </summary>
    public enum PolarEstimationRule
    {
        /// <summary>
        /// Use the day at latitude 45° in the same hemisphere.
        /// Resolution of the Islamic Fiqh Council (Muslim World League), 9th session, 1986.
        /// </summary>
        ReferenceLatitude = 0,

        /// <summary>
        /// Use the closest latitude toward the equator where every time can be calculated
        /// ("the nearest place with normal days and nights").
        /// </summary>
        NearestNormalLatitude = 1,

        /// <summary>
        /// Use the day at Mecca's latitude (21.42° N), anchored at local solar noon.
        /// </summary>
        Mecca = 2
    }
}
