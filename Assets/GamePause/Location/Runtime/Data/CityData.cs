using System;

namespace GamePause.Location
{
    /// <summary>
    /// Represents a city with coordinates
    /// </summary>
    [Serializable]
    public class CityData
    {
        public string Name;
        public double Latitude;
        public double Longitude;
        public string Timezone;

        public CityData() { }

        public CityData(string name, double latitude, double longitude, string timezone = "")
        {
            Name = name;
            Latitude = latitude;
            Longitude = longitude;
            Timezone = timezone;
        }
    }
}
