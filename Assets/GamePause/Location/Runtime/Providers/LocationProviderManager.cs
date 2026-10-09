using System;
using UnityEngine;

namespace GamePause.Location
{
    /// <summary>
    /// Manages location providers with persistence, rate limiting, and manual location support
    /// </summary>
    public class LocationProviderManager
    {
        private ILocationProvider _currentProvider;
        private ManualLocationProvider _manualProvider;
        private readonly MonoBehaviour _coroutineRunner;
        private LocationProviderType _activeProviderType;
        private LocationPersistenceSettings _persistenceSettings;
        private LocationDatabase _locationDatabase;
        private LocationData _cachedLocation;

        public LocationFetchStatus Status { get; private set; } = LocationFetchStatus.NotStarted;
        public LocationData CurrentLocation => _cachedLocation.IsValid ? _cachedLocation : (_currentProvider?.CurrentLocation ?? LocationData.Invalid);
        public bool IsLocationAvailable => CurrentLocation.IsValid;
        public LocationProviderType ActiveProviderType => _activeProviderType;
        public LocationDatabase LocationDatabase => _locationDatabase;

        public event Action<LocationData> OnLocationFetched;
        public event Action<string> OnLocationFetchFailed;

        public LocationProviderManager(MonoBehaviour coroutineRunner, LocationProviderType providerType = LocationProviderType.Auto, LocationPersistenceSettings persistenceSettings = null)
        {
            _coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
            _persistenceSettings = persistenceSettings ?? new LocationPersistenceSettings();
            _locationDatabase = LocationPersistence.LoadLocationDatabase();

            // Initialize manual provider (always available for manual location setting)
            _manualProvider = new ManualLocationProvider(_locationDatabase);
            _manualProvider.OnLocationFetched += HandleProviderLocationFetched;
            _manualProvider.OnLocationFetchFailed += HandleLocationFetchFailed;

            // Try to load cached location
            if (_persistenceSettings.EnablePersistence)
            {
                _cachedLocation = LocationPersistence.LoadLocation();
                if (_cachedLocation.IsValid)
                {
                    _manualProvider.LoadLocation(_cachedLocation);
                }
            }

            InitializeProvider(providerType);
        }

        private void InitializeProvider(LocationProviderType requestedType)
        {
            if (requestedType == LocationProviderType.Manual)
            {
                _activeProviderType = LocationProviderType.Manual;
                _currentProvider = _manualProvider;
                return;
            }

            _activeProviderType = requestedType == LocationProviderType.Auto
                ? DetermineOptimalProviderType()
                : requestedType;

            _currentProvider = CreateProvider(_activeProviderType);

            if (_currentProvider != null && _currentProvider != _manualProvider)
            {
                _currentProvider.OnLocationFetched += HandleProviderLocationFetched;
                _currentProvider.OnLocationFetchFailed += HandleLocationFetchFailed;
            }
        }

        private LocationProviderType DetermineOptimalProviderType()
        {
            // Check for Unity Remote connection first (Editor with Android/iOS target)
            if (MobileLocationProvider.ShouldUseMobileLocation())
            {
                return LocationProviderType.Mobile;
            }

            #if UNITY_ANDROID || UNITY_IOS
                return LocationProviderType.Mobile;
            #elif UNITY_STANDALONE || UNITY_WEBGL || UNITY_EDITOR
                return LocationProviderType.Desktop;
            #else
                return LocationProviderType.Desktop;
            #endif
        }

        private ILocationProvider CreateProvider(LocationProviderType providerType)
        {
            switch (providerType)
            {
                case LocationProviderType.Mobile:
                    return new MobileLocationProvider(_coroutineRunner);

                case LocationProviderType.Manual:
                    return _manualProvider;

                case LocationProviderType.Desktop:
                default:
                    return new DesktopLocationProvider(_coroutineRunner);
            }
        }

        /// <summary>
        /// Start fetching location data
        /// </summary>
        /// <param name="forceUpdate">Bypass rate limiting if enabled</param>
        public void FetchLocation(bool forceUpdate = false)
        {
            if (_activeProviderType == LocationProviderType.Manual)
            {
                OnLocationFetchFailed?.Invoke("Cannot auto-fetch location when in Manual mode. Use SetManualLocation instead.");
                return;
            }

            // Check rate limiting
            if (!forceUpdate && IsRateLimited())
            {
                Status = LocationFetchStatus.RateLimited;
                
                double hoursRemaining = GetHoursUntilNextUpdate();
                OnLocationFetchFailed?.Invoke($"Location update rate limited. Next update available in {hoursRemaining:F1} hours. Use cached location or force update.");
                
                // Return cached location if available
                if (_cachedLocation.IsValid)
                {
                    Status = LocationFetchStatus.LoadedFromCache;
                    OnLocationFetched?.Invoke(_cachedLocation);
                }
                return;
            }

            Status = LocationFetchStatus.Fetching;
            _currentProvider?.StartFetchingLocation();
        }

        /// <summary>
        /// Stop any ongoing location fetch operation
        /// </summary>
        public void StopFetching()
        {
            _currentProvider?.StopFetchingLocation();
        }

        /// <summary>
        /// Set location manually using country and city selection
        /// </summary>
        public void SetManualLocation(string countryName, string cityName)
        {
            _manualProvider.SetLocation(countryName, cityName);
        }

        /// <summary>
        /// Set location manually using coordinates
        /// </summary>
        public void SetManualLocation(double latitude, double longitude, string city = "", string country = "", string timezone = "")
        {
            _manualProvider.SetLocation(latitude, longitude, city, country, timezone);
        }

        /// <summary>
        /// Load cached location from persistence
        /// </summary>
        public bool LoadCachedLocation()
        {
            if (!_persistenceSettings.EnablePersistence)
            {
                return false;
            }

            _cachedLocation = LocationPersistence.LoadLocation();
            
            if (_cachedLocation.IsValid)
            {
                _manualProvider.LoadLocation(_cachedLocation);
                Status = LocationFetchStatus.LoadedFromCache;
                OnLocationFetched?.Invoke(_cachedLocation);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Clear cached location data
        /// </summary>
        public void ClearCachedLocation()
        {
            _cachedLocation = LocationData.Invalid;
            LocationPersistence.ClearSavedLocation();
        }

        /// <summary>
        /// Switch to a different provider type
        /// </summary>
        public void SwitchProvider(LocationProviderType newProviderType)
        {
            if (_activeProviderType == newProviderType && _currentProvider != null)
                return;

            CleanupCurrentProvider();
            InitializeProvider(newProviderType);
        }

        /// <summary>
        /// Update persistence settings
        /// </summary>
        public void UpdatePersistenceSettings(LocationPersistenceSettings settings)
        {
            _persistenceSettings = settings ?? new LocationPersistenceSettings();
        }

        /// <summary>
        /// Check if location update is rate limited
        /// </summary>
        public bool IsRateLimited()
        {
            if (!_persistenceSettings.EnableRateLimiting)
                return false;

            if (!_cachedLocation.IsValid)
                return false;

            // Manual locations can bypass rate limiting if setting is enabled
            if (_cachedLocation.IsManuallySet && _persistenceSettings.AllowManualBypass)
                return false;

            double hoursSinceLastUpdate = GetHoursSinceLastUpdate();
            return hoursSinceLastUpdate < _persistenceSettings.RateLimitHours;
        }

        /// <summary>
        /// Get hours since last location update
        /// </summary>
        public double GetHoursSinceLastUpdate()
        {
            if (!_cachedLocation.IsValid || _cachedLocation.LastUpdateTimestamp == 0)
                return double.MaxValue;

            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double secondsSinceUpdate = currentTimestamp - _cachedLocation.LastUpdateTimestamp;
            return secondsSinceUpdate / 3600.0;
        }

        /// <summary>
        /// Get hours until next update is allowed
        /// </summary>
        public double GetHoursUntilNextUpdate()
        {
            double hoursSinceUpdate = GetHoursSinceLastUpdate();
            return Math.Max(0, _persistenceSettings.RateLimitHours - hoursSinceUpdate);
        }

        private void HandleProviderLocationFetched(LocationData location)
        {
            _cachedLocation = location;

            if (_persistenceSettings.EnablePersistence)
            {
                LocationPersistence.SaveLocation(_cachedLocation);
            }

            Status = LocationFetchStatus.Success;
            OnLocationFetched?.Invoke(location);
        }

        private void HandleLocationFetchFailed(string error)
        {
            Status = LocationFetchStatus.Failed;
            OnLocationFetchFailed?.Invoke(error);
        }

        private void CleanupCurrentProvider()
        {
            if (_currentProvider != null && _currentProvider != _manualProvider)
            {
                _currentProvider.OnLocationFetched -= HandleProviderLocationFetched;
                _currentProvider.OnLocationFetchFailed -= HandleLocationFetchFailed;
                _currentProvider.StopFetchingLocation();
            }
            _currentProvider = null;
        }

        /// <summary>
        /// Cleanup and dispose of resources
        /// </summary>
        public void Dispose()
        {
            CleanupCurrentProvider();
            
            if (_manualProvider != null)
            {
                _manualProvider.OnLocationFetched -= HandleProviderLocationFetched;
                _manualProvider.OnLocationFetchFailed -= HandleLocationFetchFailed;
                _manualProvider = null;
            }
        }
    }
}