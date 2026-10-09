using System;

namespace GamePause.Location
{
    /// <summary>
    /// Represents geographical location data
    /// </summary>
    [Serializable]
    public struct LocationData
    {
        public double Latitude;
        public double Longitude;
        public string City;
        public string Country;
        public string Timezone;
        public bool IsValid;
        public bool IsManuallySet;
        public long LastUpdateTimestamp;

        public LocationData(double latitude, double longitude, string city = "", string country = "", string timezone = "", bool isManual = false)
        {
            Latitude = latitude;
            Longitude = longitude;
            City = city;
            Country = country;
            Timezone = timezone;
            IsValid = true;
            IsManuallySet = isManual;
            LastUpdateTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static LocationData Invalid => new LocationData
        {
            Latitude = 0,
            Longitude = 0,
            City = "",
            Country = "",
            Timezone = "",
            IsValid = false,
            IsManuallySet = false,
            LastUpdateTimestamp = 0
        };

        public DateTime LastUpdateDateTime => DateTimeOffset.FromUnixTimeSeconds(LastUpdateTimestamp).LocalDateTime;

        public override string ToString()
        {
            if (!IsValid)
                return "Invalid Location";

            string source = IsManuallySet ? "(Manual)" : "(Auto)";
            return $"Location {source}: {City}, {Country}\nCoordinates: ({Latitude:F4}, {Longitude:F4})\nTimezone: {Timezone}\nLast Update: {LastUpdateDateTime:g}";
        }
    }
}
