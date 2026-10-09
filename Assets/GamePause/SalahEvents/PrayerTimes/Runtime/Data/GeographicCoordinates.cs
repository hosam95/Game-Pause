using System;

namespace GamePause.SalahEvents.PrayerTimes.Data
{
    /// <summary>
    /// Immutable value object representing a geographic location.
    /// </summary>
    public readonly struct GeographicCoordinates : IEquatable<GeographicCoordinates>
    {
        /// <summary>
        /// Latitude in degrees. Range: -90 to 90.
        /// Positive values are North, negative are South.
        /// </summary>
        public double Latitude { get; }
        
        /// <summary>
        /// Longitude in degrees. Range: -180 to 180.
        /// Positive values are East, negative are West.
        /// </summary>
        public double Longitude { get; }
        
        public GeographicCoordinates(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
        
        public bool Equals(GeographicCoordinates other)
        {
            return Math.Abs(Latitude - other.Latitude) < 0.0001 &&
                   Math.Abs(Longitude - other.Longitude) < 0.0001;
        }
        
        public override bool Equals(object obj)
        {
            return obj is GeographicCoordinates other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(
                Math.Round(Latitude, 4),
                Math.Round(Longitude, 4)
            );
        }
        
        public override string ToString()
        {
            return $"({Latitude:F4}, {Longitude:F4})";
        }
        
        public static bool operator ==(GeographicCoordinates left, GeographicCoordinates right)
        {
            return left.Equals(right);
        }
        
        public static bool operator !=(GeographicCoordinates left, GeographicCoordinates right)
        {
            return !left.Equals(right);
        }
    }
}