using System;
using UnityEngine;

namespace GamePause.Location
{
    /// <summary>
    /// Location provider for manual location selection via country/city database
    /// </summary>
    public class ManualLocationProvider : ILocationProvider
    {
        public LocationFetchStatus Status { get; private set; } = LocationFetchStatus.NotStarted;
        public LocationData CurrentLocation { get; private set; } = LocationData.Invalid;
        public bool IsLocationAvailable => Status == LocationFetchStatus.Success && CurrentLocation.IsValid;

        public event Action<LocationData> OnLocationFetched;
        public event Action<string> OnLocationFetchFailed;

        private readonly LocationDatabase _locationDatabase;

        public LocationDatabase Database => _locationDatabase;

        public ManualLocationProvider(LocationDatabase database = null)
        {
            _locationDatabase = database ?? LocationPersistence.LoadLocationDatabase();
        }

        public void StartFetchingLocation()
        {
            // Manual provider doesn't auto-fetch, inform user to set location manually
            OnLocationFetchFailed?.Invoke("Manual location provider requires explicit location setting. Use SetLocation method.");
        }

        public void StopFetchingLocation()
        {
            // Nothing to stop for manual provider
        }

        /// <summary>
        /// Set location using country and city names from the database
        /// </summary>
        public bool SetLocation(string countryName, string cityName)
        {
            CountryData country = _locationDatabase.Countries.Find(c => c.Name == countryName);
            if (country == null)
            {
                Status = LocationFetchStatus.Failed;
                OnLocationFetchFailed?.Invoke($"Country not found: {countryName}");
                return false;
            }

            CityData city = country.Cities.Find(c => c.Name == cityName);
            if (city == null)
            {
                Status = LocationFetchStatus.Failed;
                OnLocationFetchFailed?.Invoke($"City not found: {cityName} in {countryName}");
                return false;
            }

            return SetLocation(city.Latitude, city.Longitude, cityName, countryName, city.Timezone);
        }

        /// <summary>
        /// Set location using direct coordinates
        /// </summary>
        public bool SetLocation(double latitude, double longitude, string city = "", string country = "", string timezone = "")
        {
            CurrentLocation = new LocationData(
                latitude,
                longitude,
                city,
                country,
                string.IsNullOrEmpty(timezone) ? GetSystemTimezone() : timezone,
                true
            );

            Status = LocationFetchStatus.Success;
            OnLocationFetched?.Invoke(CurrentLocation);
            return true;
        }

        /// <summary>
        /// Load a previously set location
        /// </summary>
        public void LoadLocation(LocationData location)
        {
            if (location.IsValid)
            {
                CurrentLocation = location;
                Status = LocationFetchStatus.Success;
            }
        }

        private string GetSystemTimezone()
        {
            try
            {
                return TimeZoneInfo.Local.Id;
            }
            catch
            {
                return "";
            }
        }
    }
}