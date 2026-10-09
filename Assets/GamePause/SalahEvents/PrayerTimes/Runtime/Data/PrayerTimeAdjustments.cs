using System;

namespace GamePause.SalahEvents.PrayerTimes.Data
{
    /// <summary>
    /// Manual adjustments in minutes for each prayer time.
    /// </summary>
    public readonly struct PrayerTimeAdjustments : IEquatable<PrayerTimeAdjustments>
    {
        public int Fajr { get; }
        public int Sunrise { get; }
        public int Dhuhr { get; }
        public int Asr { get; }
        public int Maghrib { get; }
        public int Isha { get; }
        
        public PrayerTimeAdjustments(
            int fajr = 0,
            int sunrise = 0,
            int dhuhr = 0,
            int asr = 0,
            int maghrib = 0,
            int isha = 0)
        {
            Fajr = fajr;
            Sunrise = sunrise;
            Dhuhr = dhuhr;
            Asr = asr;
            Maghrib = maghrib;
            Isha = isha;
        }
        
        /// <summary>
        /// Default adjustments with all zeros.
        /// </summary>
        public static PrayerTimeAdjustments None => new PrayerTimeAdjustments();
        
        public bool Equals(PrayerTimeAdjustments other)
        {
            return Fajr == other.Fajr &&
                   Sunrise == other.Sunrise &&
                   Dhuhr == other.Dhuhr &&
                   Asr == other.Asr &&
                   Maghrib == other.Maghrib &&
                   Isha == other.Isha;
        }
        
        public override bool Equals(object obj)
        {
            return obj is PrayerTimeAdjustments other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Fajr, Sunrise, Dhuhr, Asr, Maghrib, Isha);
        }
    }
}