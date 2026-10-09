using System;
using System.Collections.Generic;

namespace GamePause.Location
{
    /// <summary>
    /// Database of countries and cities for manual selection
    /// </summary>
    [Serializable]
    public class LocationDatabase
    {
        public List<CountryData> Countries = new List<CountryData>();

        /// <summary>
        /// Create a default database with sample data
        /// </summary>
        public static LocationDatabase CreateDefault()
        {
            var database = new LocationDatabase();

            // Add sample countries and cities (expand as needed)
            database.Countries.Add(new CountryData
            {
                Name = "Saudi Arabia",
                Code = "SA",
                Timezone = "Asia/Riyadh",
                Cities = new List<CityData>
                {
                    new CityData("Makkah", 21.4225, 39.8262, "Asia/Riyadh"),
                    new CityData("Madinah", 24.5247, 39.5692, "Asia/Riyadh"),
                    new CityData("Riyadh", 24.7136, 46.6753, "Asia/Riyadh"),
                    new CityData("Jeddah", 21.5433, 39.1728, "Asia/Riyadh"),
                    new CityData("Dammam", 26.4207, 50.0888, "Asia/Riyadh")
                }
            });

            database.Countries.Add(new CountryData
            {
                Name = "United Arab Emirates",
                Code = "AE",
                Timezone = "Asia/Dubai",
                Cities = new List<CityData>
                {
                    new CityData("Dubai", 25.2048, 55.2708, "Asia/Dubai"),
                    new CityData("Abu Dhabi", 24.4539, 54.3773, "Asia/Dubai"),
                    new CityData("Sharjah", 25.3463, 55.4209, "Asia/Dubai")
                }
            });

            database.Countries.Add(new CountryData
            {
                Name = "Egypt",
                Code = "EG",
                Timezone = "Africa/Cairo",
                Cities = new List<CityData>
                {
                    new CityData("Cairo", 30.0444, 31.2357, "Africa/Cairo"),
                    new CityData("Alexandria", 31.2001, 29.9187, "Africa/Cairo"),
                    new CityData("Giza", 30.0131, 31.2089, "Africa/Cairo")
                }
            });

            database.Countries.Add(new CountryData
            {
                Name = "United States",
                Code = "US",
                Timezone = "America/New_York",
                Cities = new List<CityData>
                {
                    new CityData("New York", 40.7128, -74.0060, "America/New_York"),
                    new CityData("Los Angeles", 34.0522, -118.2437, "America/Los_Angeles"),
                    new CityData("Chicago", 41.8781, -87.6298, "America/Chicago"),
                    new CityData("Houston", 29.7604, -95.3698, "America/Chicago"),
                    new CityData("Dearborn", 42.3223, -83.1763, "America/Detroit")
                }
            });

            database.Countries.Add(new CountryData
            {
                Name = "United Kingdom",
                Code = "GB",
                Timezone = "Europe/London",
                Cities = new List<CityData>
                {
                    new CityData("London", 51.5074, -0.1278, "Europe/London"),
                    new CityData("Birmingham", 52.4862, -1.8904, "Europe/London"),
                    new CityData("Manchester", 53.4808, -2.2426, "Europe/London")
                }
            });

            database.Countries.Add(new CountryData
            {
                Name = "Turkey",
                Code = "TR",
                Timezone = "Europe/Istanbul",
                Cities = new List<CityData>
                {
                    new CityData("Istanbul", 41.0082, 28.9784, "Europe/Istanbul"),
                    new CityData("Ankara", 39.9334, 32.8597, "Europe/Istanbul"),
                    new CityData("Izmir", 38.4192, 27.1287, "Europe/Istanbul")
                }
            });

            database.Countries.Add(new CountryData
            {
                Name = "Malaysia",
                Code = "MY",
                Timezone = "Asia/Kuala_Lumpur",
                Cities = new List<CityData>
                {
                    new CityData("Kuala Lumpur", 3.1390, 101.6869, "Asia/Kuala_Lumpur"),
                    new CityData("George Town", 5.4141, 100.3288, "Asia/Kuala_Lumpur"),
                    new CityData("Johor Bahru", 1.4927, 103.7414, "Asia/Kuala_Lumpur")
                }
            });

            database.Countries.Add(new CountryData
            {
                Name = "Indonesia",
                Code = "ID",
                Timezone = "Asia/Jakarta",
                Cities = new List<CityData>
                {
                    new CityData("Jakarta", -6.2088, 106.8456, "Asia/Jakarta"),
                    new CityData("Surabaya", -7.2575, 112.7521, "Asia/Jakarta"),
                    new CityData("Bandung", -6.9175, 107.6191, "Asia/Jakarta")
                }
            });

            database.Countries.Add(new CountryData
            {
                Name = "Pakistan",
                Code = "PK",
                Timezone = "Asia/Karachi",
                Cities = new List<CityData>
                {
                    new CityData("Karachi", 24.8607, 67.0011, "Asia/Karachi"),
                    new CityData("Lahore", 31.5204, 74.3587, "Asia/Karachi"),
                    new CityData("Islamabad", 33.6844, 73.0479, "Asia/Karachi")
                }
            });

            database.Countries.Add(new CountryData
            {
                Name = "Bangladesh",
                Code = "BD",
                Timezone = "Asia/Dhaka",
                Cities = new List<CityData>
                {
                    new CityData("Dhaka", 23.8103, 90.4125, "Asia/Dhaka"),
                    new CityData("Chittagong", 22.3569, 91.7832, "Asia/Dhaka"),
                    new CityData("Khulna", 22.8456, 89.5403, "Asia/Dhaka")
                }
            });

            return database;
        }
    }
}
