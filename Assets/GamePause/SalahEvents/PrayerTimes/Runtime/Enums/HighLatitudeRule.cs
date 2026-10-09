namespace GamePause.SalahEvents.PrayerTimes.Enums
{
    /// <summary>
    /// Rules for Fajr and Isha where the sun still rises and sets but twilight lasts too long
    /// or never ends (roughly 48°–66°). Polar day or night is handled by <see cref="PolarEstimationRule"/>.
    /// </summary>
    public enum HighLatitudeRule
    {
        /// <summary>
        /// No adjustment. Times the sun never reaches are estimated with <see cref="PolarEstimationRule"/>.
        /// </summary>
        None = 0,

        /// <summary>
        /// Fajr and Isha no further than half the night (sunset to sunrise) from sunrise / sunset.
        /// Historical practice reported for Bulghār by Ibn ʿĀbidīn and al-Marjānī; no institutional ruling found.
        /// </summary>
        MiddleOfNight = 1,

        /// <summary>
        /// Isha no later than 1/7 of the night after sunset, Fajr no earlier than 1/7 before sunrise.
        /// Attributed to Hanafi authorities (Ashraf Ali Thanwi, Ibn ʿĀbidīn); used by the Moonsighting Committee.
        /// </summary>
        SeventhOfNight = 2,

        /// <summary>
        /// The limit is angle/60 of the night (PrayTimes.org). Used by AlAdhan, the accuracy reference.
        /// No institutional ruling found. Default.
        /// </summary>
        AngleBased = 3,

        /// <summary>
        /// When Fajr or Isha is not reached, it takes the same fraction of the local night as at
        /// latitude 45° on the same meridian. Islamic Fiqh Council (Muslim World League), 9th session,
        /// Rajab 1406 AH, decree 8, region 2.
        /// </summary>
        NightFractionAt45 = 4
    }
}
