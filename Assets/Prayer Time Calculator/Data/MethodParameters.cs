using System;

namespace GamePause.PrayerTimeCalculator.Data
{
    /// <summary>
    /// Immutable parameters defining a calculation method's angles and intervals.
    /// </summary>
    public readonly struct MethodParameters : IEquatable<MethodParameters>
    {
        /// <summary>
        /// Sun angle below horizon for Fajr in degrees.
        /// </summary>
        public double FajrAngle { get; }
        
        /// <summary>
        /// Sun angle below horizon for Isha in degrees.
        /// Null if using fixed interval.
        /// </summary>
        public double? IshaAngle { get; }
        
        /// <summary>
        /// Fixed interval in minutes after Maghrib for Isha.
        /// Null if using angle-based calculation.
        /// </summary>
        public int? IshaIntervalMinutes { get; }
        
        /// <summary>
        /// Sun angle below horizon for Maghrib in degrees.
        /// Default is 0 (at geometric sunset).
        /// Some methods (Tehran, Qom) use a specific angle.
        /// </summary>
        public double MaghribAngle { get; }
        
        /// <summary>
        /// Fixed interval in minutes after sunset for Maghrib.
        /// Null if using angle-based calculation.
        /// </summary>
        public int? MaghribMinutes { get; }
        
        /// <summary>
        /// Whether Isha uses fixed interval instead of angle.
        /// </summary>
        public bool UsesIshaInterval => IshaIntervalMinutes.HasValue;
        
        /// <summary>
        /// Whether Maghrib uses fixed interval instead of angle.
        /// </summary>
        public bool UsesMaghribInterval => MaghribMinutes.HasValue;
        
        /// <summary>
        /// Creates method parameters with angle-based Isha.
        /// </summary>
        public MethodParameters(
            double fajrAngle, 
            double ishaAngle,
            double maghribAngle = 0,
            int? maghribMinutes = null)
        {
            FajrAngle = fajrAngle;
            IshaAngle = ishaAngle;
            IshaIntervalMinutes = null;
            MaghribAngle = maghribAngle;
            MaghribMinutes = maghribMinutes;
        }
        
        /// <summary>
        /// Creates method parameters with fixed interval Isha.
        /// </summary>
        public MethodParameters(
            double fajrAngle, 
            int ishaIntervalMinutes,
            double maghribAngle = 0,
            int? maghribMinutes = null)
        {
            FajrAngle = fajrAngle;
            IshaAngle = null;
            IshaIntervalMinutes = ishaIntervalMinutes;
            MaghribAngle = maghribAngle;
            MaghribMinutes = maghribMinutes;
        }
        
        public bool Equals(MethodParameters other)
        {
            return Math.Abs(FajrAngle - other.FajrAngle) < 0.001 &&
                   IshaAngle.Equals(other.IshaAngle) &&
                   IshaIntervalMinutes.Equals(other.IshaIntervalMinutes) &&
                   Math.Abs(MaghribAngle - other.MaghribAngle) < 0.001 &&
                   MaghribMinutes.Equals(other.MaghribMinutes);
        }
        
        public override bool Equals(object obj)
        {
            return obj is MethodParameters other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(FajrAngle, IshaAngle, IshaIntervalMinutes, MaghribAngle, MaghribMinutes);
        }
        
        public override string ToString()
        {
            string ishaStr = UsesIshaInterval 
                ? $"Isha: {IshaIntervalMinutes}min" 
                : $"Isha: {IshaAngle}°";
            
            string maghribStr = MaghribAngle > 0 
                ? $", Maghrib: {MaghribAngle}°" 
                : "";
            
            return $"Fajr: {FajrAngle}°, {ishaStr}{maghribStr}";
        }
    }
}