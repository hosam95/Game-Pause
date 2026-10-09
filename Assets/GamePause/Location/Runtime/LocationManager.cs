using UnityEngine;
using GamePause.Debugging;
using System.Collections.Generic;

namespace GamePause.Location
{
    /// <summary>
    /// MonoBehaviour manager for location fetching with manual selection, persistence, and rate limiting
    /// </summary>
    public class LocationManager : MonoBehaviour
    {
        [Header("Provider Configuration")]
        [SerializeField]
        [Tooltip("Type of location provider to use")]
        private LocationProviderType _providerType = LocationProviderType.Auto;

        [SerializeField]
        [Tooltip("Automatically fetch location when the scene starts")]
        private bool _fetchOnStart = true;

        [Header("Retry Settings")]
        [SerializeField]
        [Tooltip("Automatically retry on failure")]
        private bool _retryOnFailure = false;

        [SerializeField]
        [Tooltip("Delay before retrying (in seconds)")]
        private float _retryDelay = 5f;

        [SerializeField]
        [Tooltip("Maximum number of retry attempts (0 = unlimited)")]
        private int _maxRetryAttempts = 3;

        [Header("Persistence Settings")]
        [SerializeField]
        [Tooltip("Persistence and rate limiting settings")]
        private LocationPersistenceSettings _persistenceSettings = new LocationPersistenceSettings();

        [Header("Manual Location Selection")]
        [SerializeField]
        [Tooltip("Currently selected country for manual location")]
        private string _selectedCountry;

        [SerializeField]
        [Tooltip("Currently selected city for manual location")]
        private string _selectedCity;

        private LocationProviderManager _providerManager;
        private bool _isInitialized;
        private int _currentRetryCount;

        #region Public Properties

        public LocationData CurrentLocation => _providerManager?.CurrentLocation ?? LocationData.Invalid;
        public LocationFetchStatus Status => _providerManager?.Status ?? LocationFetchStatus.NotStarted;
        public bool IsLocationAvailable => _providerManager?.IsLocationAvailable ?? false;
        public LocationProviderType ActiveProviderType => _providerManager?.ActiveProviderType ?? LocationProviderType.Auto;
        public LocationDatabase LocationDatabase => _providerManager?.LocationDatabase;
        public bool IsRateLimited => _providerManager?.IsRateLimited() ?? false;
        public double HoursUntilNextUpdate => _providerManager?.GetHoursUntilNextUpdate() ?? 0;
        public LocationPersistenceSettings PersistenceSettings => _persistenceSettings;
        public int CurrentRetryCount => _currentRetryCount;
        public int MaxRetryAttempts => _maxRetryAttempts;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();

            if (_fetchOnStart)
            {
                // Try to load cached location first
                if (_persistenceSettings.EnablePersistence && _providerManager.LoadCachedLocation())
                {
                    DebugLogger.Log("<color=#00FF00>[LocationManager]</color> Loaded location from cache.");
                    
                    // Check if we should also refresh the location
                    if (!_providerManager.IsRateLimited())
                    {
                        DebugLogger.Log("<color=#00FF00>[LocationManager]</color> Refreshing location data...");
                        FetchLocation(false);
                    }
                }
                else
                {
                    FetchLocation(false);
                }
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {
            if (_isInitialized)
            {
                DebugLogger.LogWarning("[LocationManager] Already initialized.");
                return;
            }

            _providerManager = new LocationProviderManager(this, _providerType, _persistenceSettings);
            _providerManager.OnLocationFetched += HandleLocationFetched;
            _providerManager.OnLocationFetchFailed += HandleLocationFetchFailed;

            _isInitialized = true;
            _currentRetryCount = 0;

            DebugLogger.Log($"<color=#00FF00>[LocationManager]</color> Initialized with provider: <b>{_providerManager.ActiveProviderType}</b>");
            
            if (_persistenceSettings.EnableRateLimiting)
            {
                DebugLogger.Log($"<color=#00FF00>[LocationManager]</color> Rate limiting enabled: {_persistenceSettings.RateLimitHours} hours between updates");
            }
        }

        /// <summary>
        /// Start fetching location
        /// </summary>
        /// <param name="forceUpdate">Bypass rate limiting</param>
        public void FetchLocation(bool forceUpdate = false)
        {
            if (!_isInitialized)
            {
                DebugLogger.LogError("[LocationManager] Cannot fetch location - not initialized.");
                return;
            }

            // Reset retry count for new fetch request
            _currentRetryCount = 0;

            if (forceUpdate)
            {
                DebugLogger.Log("<color=#00FF00>[LocationManager]</color> Force fetching location (bypassing rate limit)...");
            }
            else
            {
                DebugLogger.Log("<color=#00FF00>[LocationManager]</color> Starting location fetch...");
            }

            _providerManager.FetchLocation(forceUpdate);
        }

        /// <summary>
        /// Stop fetching location
        /// </summary>
        public void StopFetching()
        {
            if (!_isInitialized)
                return;

            CancelInvoke(nameof(RetryFetch));
            _currentRetryCount = 0;

            DebugLogger.Log("<color=#00FF00>[LocationManager]</color> Stopping location fetch...");
            _providerManager.StopFetching();
        }

        /// <summary>
        /// Set location manually using country and city selection
        /// </summary>
        public void SetManualLocation(string countryName, string cityName)
        {
            if (!_isInitialized)
            {
                DebugLogger.LogError("[LocationManager] Cannot set manual location - not initialized.");
                return;
            }

            DebugLogger.Log($"<color=#00FF00>[LocationManager]</color> Setting manual location: {cityName}, {countryName}");
            _providerManager.SetManualLocation(countryName, cityName);
        }

        /// <summary>
        /// Set location manually using coordinates
        /// </summary>
        public void SetManualLocationCoordinates(double latitude, double longitude, string city = "", string country = "", string timezone = "")
        {
            if (!_isInitialized)
            {
                DebugLogger.LogError("[LocationManager] Cannot set manual location - not initialized.");
                return;
            }

            DebugLogger.Log($"<color=#00FF00>[LocationManager]</color> Setting manual coordinates: ({latitude:F4}, {longitude:F4})");
            _providerManager.SetManualLocation(latitude, longitude, city, country, timezone);
        }

        /// <summary>
        /// Apply the currently selected country and city from inspector fields
        /// </summary>
        public void ApplySelectedManualLocation()
        {
            if (string.IsNullOrEmpty(_selectedCountry) || string.IsNullOrEmpty(_selectedCity))
            {
                DebugLogger.LogWarning("[LocationManager] Please select both country and city.");
                return;
            }

            SetManualLocation(_selectedCountry, _selectedCity);
        }

        /// <summary>
        /// Get list of available countries
        /// </summary>
        public List<string> GetAvailableCountries()
        {
            List<string> countries = new List<string>();
            if (_providerManager?.LocationDatabase != null)
            {
                foreach (var country in _providerManager.LocationDatabase.Countries)
                {
                    countries.Add(country.Name);
                }
            }
            return countries;
        }

        /// <summary>
        /// Get list of cities for a given country
        /// </summary>
        public List<string> GetCitiesForCountry(string countryName)
        {
            List<string> cities = new List<string>();
            if (_providerManager?.LocationDatabase != null)
            {
                var country = _providerManager.LocationDatabase.Countries.Find(c => c.Name == countryName);
                if (country != null)
                {
                    foreach (var city in country.Cities)
                    {
                        cities.Add(city.Name);
                    }
                }
            }
            return cities;
        }

        /// <summary>
        /// Switch to a different provider type
        /// </summary>
        public void SwitchProvider(LocationProviderType newType)
        {
            if (!_isInitialized)
            {
                DebugLogger.LogError("[LocationManager] Cannot switch provider - not initialized.");
                return;
            }

            DebugLogger.Log($"<color=#00FF00>[LocationManager]</color> Switching provider to: <b>{newType}</b>");
            _providerManager.SwitchProvider(newType);
        }

        /// <summary>
        /// Clear cached location data
        /// </summary>
        public void ClearCachedLocation()
        {
            if (!_isInitialized)
                return;

            DebugLogger.Log("<color=#00FF00>[LocationManager]</color> Clearing cached location data...");
            _providerManager.ClearCachedLocation();
        }

        /// <summary>
        /// Update persistence settings at runtime
        /// </summary>
        public void UpdatePersistenceSettings(LocationPersistenceSettings settings)
        {
            _persistenceSettings = settings;
            _providerManager?.UpdatePersistenceSettings(settings);
            DebugLogger.Log("<color=#00FF00>[LocationManager]</color> Persistence settings updated.");
        }

        /// <summary>
        /// Reset retry counter to allow retries again
        /// </summary>
        public void ResetRetryCount()
        {
            _currentRetryCount = 0;
        }

        #endregion

        #region Private Methods

        private void HandleLocationFetched(LocationData location)
        {
            _currentRetryCount = 0; // Reset retry count on success
            string source = location.IsManuallySet ? "Manual" : "Auto";
            DebugLogger.Log($"<color=#00FF00>[LocationManager]</color> Location fetched successfully ({source})!\n{location}");
        }

        private void HandleLocationFetchFailed(string error)
        {
            DebugLogger.LogError($"[LocationManager] Location fetch failed: {error}");

            // Don't retry for certain failure types
            LocationFetchStatus status = _providerManager?.Status ?? LocationFetchStatus.Failed;
            bool shouldRetry = _retryOnFailure && 
                               status != LocationFetchStatus.RateLimited &&
                               status != LocationFetchStatus.PermissionDenied;

            if (shouldRetry)
            {
                // Check retry limit
                if (_maxRetryAttempts > 0 && _currentRetryCount >= _maxRetryAttempts)
                {
                    DebugLogger.LogWarning($"[LocationManager] Maximum retry attempts ({_maxRetryAttempts}) reached. Stopping retries.");
                    return;
                }

                _currentRetryCount++;
                DebugLogger.LogWarning($"[LocationManager] Retrying in {_retryDelay} seconds... (Attempt {_currentRetryCount}/{(_maxRetryAttempts > 0 ? _maxRetryAttempts.ToString() : "∞")})");
                Invoke(nameof(RetryFetch), _retryDelay);
            }
        }

        private void RetryFetch()
        {
            _providerManager?.FetchLocation(false);
        }

        private void Cleanup()
        {
            CancelInvoke();
            
            if (_providerManager != null)
            {
                _providerManager.OnLocationFetched -= HandleLocationFetched;
                _providerManager.OnLocationFetchFailed -= HandleLocationFetchFailed;
                _providerManager.Dispose();
                _providerManager = null;
            }

            _isInitialized = false;
        }

        #endregion

        #region Editor Context Menu

        [ContextMenu("Fetch Location")]
        private void EditorFetchLocation()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Can only fetch location in Play mode.");
                return;
            }
            FetchLocation(false);
        }

        [ContextMenu("Force Fetch Location (Bypass Rate Limit)")]
        private void EditorForceFetchLocation()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Can only fetch location in Play mode.");
                return;
            }
            FetchLocation(true);
        }

        [ContextMenu("Apply Manual Location")]
        private void EditorApplyManualLocation()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Can only set location in Play mode.");
                return;
            }
            ApplySelectedManualLocation();
        }

        [ContextMenu("Clear Cached Location")]
        private void EditorClearCachedLocation()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Can only clear cache in Play mode.");
                return;
            }
            ClearCachedLocation();
        }

        [ContextMenu("Log Available Countries")]
        private void EditorLogCountries()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Can only access database in Play mode.");
                return;
            }
            
            var countries = GetAvailableCountries();
            DebugLogger.Log($"<color=#00FF00>[LocationManager]</color> Available countries ({countries.Count}):\n{string.Join(", ", countries)}");
        }

        [ContextMenu("Log Rate Limit Status")]
        private void EditorLogRateLimitStatus()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Can only check status in Play mode.");
                return;
            }

            if (IsRateLimited)
            {
                DebugLogger.LogWarning($"[LocationManager] Rate limited. Next update in {HoursUntilNextUpdate:F1} hours.");
            }
            else
            {
                DebugLogger.Log("<color=#00FF00>[LocationManager]</color> Not rate limited. Can fetch location.");
            }
        }

        [ContextMenu("Reset Retry Count")]
        private void EditorResetRetryCount()
        {
            if (!Application.isPlaying)
                return;
            ResetRetryCount();
            DebugLogger.Log("<color=#00FF00>[LocationManager]</color> Retry count reset.");
        }

        [ContextMenu("Clear Logs")]
        private void EditorClearLogs()
        {
            if (!Application.isPlaying)
                return;
            DebugLogger.ClearLogs();
        }

        #endregion
    }
}