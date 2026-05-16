namespace GamePause.PrayerTimeCalculator.Enums
{
    /// <summary>
    /// Available prayer time calculation method presets.
    /// </summary>
    public enum CalculationMethod
    {
        /// <summary>
        /// Egyptian General Authority of Survey.
        /// Fajr: 19.5°, Isha: 17.5°
        /// Used in: Africa, Syria, Lebanon, Malaysia
        /// </summary>
        EgyptianAuthority = 0,
        
        /// <summary>
        /// Umm Al-Qura University, Mecca.
        /// Fajr: 18.5°, Isha: 90 min after Maghrib (120 min during Ramadan)
        /// Used in: Saudi Arabia
        /// </summary>
        UmmAlQura = 1,
        
        /// <summary>
        /// Muslim World League.
        /// Fajr: 18°, Isha: 17°
        /// Used in: Europe, Far East, parts of USA
        /// </summary>
        MuslimWorldLeague = 2,
        
        /// <summary>
        /// Islamic Society of North America.
        /// Fajr: 15°, Isha: 15°
        /// Used in: North America (USA, Canada)
        /// </summary>
        ISNA = 3,
        
        /// <summary>
        /// University of Islamic Sciences, Karachi.
        /// Fajr: 18°, Isha: 18°
        /// Used in: Pakistan, Bangladesh, India, Afghanistan
        /// </summary>
        Karachi = 4,
        
        /// <summary>
        /// Institute of Geophysics, University of Tehran.
        /// Fajr: 17.7°, Isha: 14°, Maghrib: 4.5°
        /// Used in: Iran
        /// </summary>
        Tehran = 5,
        
        /// <summary>
        /// Shia Ithna-Ashari, Leva Institute, Qom.
        /// Fajr: 16°, Isha: 14°, Maghrib: 4°
        /// Used in: Shia communities
        /// </summary>
        Qom = 6,
        
        /// <summary>
        /// Grand Mosque of Paris / Muslims of France.
        /// Fajr: 12°, Isha: 12°
        /// Used in: France
        /// </summary>
        MuslimsOfFrance = 7,
        
        /// <summary>
        /// Spiritual Administration of Muslims of Russia.
        /// Fajr: 16°, Isha: 15°
        /// Used in: Russia
        /// </summary>
        Russia = 8,
        
        /// <summary>
        /// Majlis Ugama Islam Singapura.
        /// Fajr: 20°, Isha: 18°
        /// Used in: Singapore
        /// </summary>
        Singapore = 9
    }
}