using System;
using System.Collections;
using UnityEngine;

namespace GamePause.Location
{
    /// <summary>
    /// Location provider that uses GPS for mobile platforms
    /// Supports Unity Remote for testing with connection waiting
    /// </summary>
    public class MobileLocationProvider : ILocationProvider
    {
        private const float DEFAULT_TIMEOUT = 30f;
        private const float DESIRED_ACCURACY = 10f;
        private const float UPDATE_DISTANCE = 10f;
        private const float REMOTE_CONNECTION_TIMEOUT = 30f;
        private const float REMOTE_CHECK_INTERVAL = 0.5f;

        public LocationFetchStatus Status { get; private set; } = LocationFetchStatus.NotStarted;
        public LocationData CurrentLocation { get; private set; } = LocationData.Invalid;
        public bool IsLocationAvailable => Status == LocationFetchStatus.Success && CurrentLocation.IsValid;

        public event Action<LocationData> OnLocationFetched;
        public event Action<string> OnLocationFetchFailed;

        private readonly MonoBehaviour _coroutineRunner;
        private readonly float _timeout;
        private Coroutine _fetchCoroutine;
        private AndroidPermissionHandler _permissionHandler;
        private bool _permissionRequestPending;
        private bool _permissionGranted;

        public MobileLocationProvider(MonoBehaviour coroutineRunner, float timeout = DEFAULT_TIMEOUT)
        {
            _coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
            _timeout = timeout;
            _permissionHandler = new AndroidPermissionHandler(coroutineRunner);
        }

        public void StartFetchingLocation()
        {
            if (Status == LocationFetchStatus.Fetching || Status == LocationFetchStatus.Initializing)
            {
                return;
            }

            Status = LocationFetchStatus.Initializing;
            _fetchCoroutine = _coroutineRunner.StartCoroutine(FetchGPSLocationCoroutine());
        }

        public void StopFetchingLocation()
        {
            if (_fetchCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_fetchCoroutine);
                _fetchCoroutine = null;
            }

            if (Input.location.status == LocationServiceStatus.Running)
            {
                Input.location.Stop();
            }

            if (Status == LocationFetchStatus.Fetching || Status == LocationFetchStatus.Initializing)
            {
                Status = LocationFetchStatus.NotStarted;
            }
        }

        private IEnumerator FetchGPSLocationCoroutine()
        {
            bool isEditorWithAndroidTarget = IsEditorWithAndroidTarget();
            
            // If in editor with Android target, wait for Unity Remote connection
            if (isEditorWithAndroidTarget)
            {
                yield return WaitForUnityRemoteConnection();
                
                if (!IsUnityRemoteConnected())
                {
                    HandleError("Unity Remote connection timed out. Please connect Unity Remote on your Android device.", 
                        LocationFetchStatus.Timeout);
                    yield break;
                }
            }

            bool isUnityRemote = IsUnityRemoteConnected();
            
            if (isUnityRemote)
            {
                // Small delay to ensure connection is stable
                yield return new WaitForSeconds(0.5f);
            }

            // Request permission on Android
            #if UNITY_ANDROID && !UNITY_EDITOR
                if (!_permissionHandler.HasLocationPermission())
                {
                    _permissionRequestPending = true;
                    _permissionGranted = false;

                    _permissionHandler.OnPermissionGranted += OnPermissionGranted;
                    _permissionHandler.OnPermissionDenied += OnPermissionDenied;
                    _permissionHandler.RequestLocationPermission();

                    // Wait for permission result
                    while (_permissionRequestPending)
                    {
                        yield return null;
                    }

                    _permissionHandler.OnPermissionGranted -= OnPermissionGranted;
                    _permissionHandler.OnPermissionDenied -= OnPermissionDenied;

                    if (!_permissionGranted)
                    {
                        HandleError("Location permission was denied. Please grant location permission in app settings.", 
                            LocationFetchStatus.PermissionDenied);
                        yield break;
                    }
                }
            #endif

            // Check if user has enabled location services
            if (!Input.location.isEnabledByUser)
            {
                string message = isUnityRemote 
                    ? "Location services are disabled on the connected device. Please enable location permissions on your mobile device."
                    : "Location services are disabled by the user. Please enable location services in device settings.";
                    
                HandleError(message, LocationFetchStatus.ServiceDisabled);
                yield break;
            }

            // Start the location service
            Input.location.Start(DESIRED_ACCURACY, UPDATE_DISTANCE);

            // Wait for initialization with progress feedback
            float elapsedTime = 0f;
            while (Input.location.status == LocationServiceStatus.Initializing && elapsedTime < _timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsedTime += 0.5f;
            }

            // Check for timeout during initialization
            if (elapsedTime >= _timeout)
            {
                Input.location.Stop();
                string message = isUnityRemote
                    ? "Location service initialization timed out. Ensure the mobile device has GPS enabled and has a clear view of the sky."
                    : "Location service initialization timed out.";
                HandleError(message, LocationFetchStatus.Timeout);
                yield break;
            }

            // Check if service failed to start
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Input.location.Stop();
                HandleError("Failed to initialize location service.", LocationFetchStatus.Failed);
                yield break;
            }

            // Service is running, fetch location
            Status = LocationFetchStatus.Fetching;

            if (Input.location.status == LocationServiceStatus.Running)
            {
                LocationInfo locationInfo = Input.location.lastData;

                // Validate location data
                if (locationInfo.timestamp <= 0)
                {
                    // Invalid data, might need to wait for first valid reading
                    yield return new WaitForSeconds(1f);
                    locationInfo = Input.location.lastData;
                }

                CurrentLocation = new LocationData(
                    locationInfo.latitude,
                    locationInfo.longitude,
                    "",
                    "",
                    GetSystemTimezone(),
                    false
                );

                Status = LocationFetchStatus.Success;
                OnLocationFetched?.Invoke(CurrentLocation);
            }
            else
            {
                HandleError("Location service is not running.", LocationFetchStatus.Failed);
            }

            // Stop location service to conserve battery
            Input.location.Stop();
            _fetchCoroutine = null;
        }

        private void OnPermissionGranted()
        {
            _permissionGranted = true;
            _permissionRequestPending = false;
        }

        private void OnPermissionDenied()
        {
            _permissionGranted = false;
            _permissionRequestPending = false;
        }

        /// <summary>
        /// Wait for Unity Remote to connect with timeout
        /// </summary>
        private IEnumerator WaitForUnityRemoteConnection()
        {
            float elapsedTime = 0f;
            
            Debug.Log("[MobileLocationProvider] Waiting for Unity Remote connection...");
            
            while (!IsUnityRemoteConnected() && elapsedTime < REMOTE_CONNECTION_TIMEOUT)
            {
                yield return new WaitForSeconds(REMOTE_CHECK_INTERVAL);
                elapsedTime += REMOTE_CHECK_INTERVAL;
            }
            
            if (IsUnityRemoteConnected())
            {
                Debug.Log("[MobileLocationProvider] Unity Remote connected successfully.");
            }
            else
            {
                Debug.LogWarning($"[MobileLocationProvider] Unity Remote connection timeout after {REMOTE_CONNECTION_TIMEOUT} seconds.");
            }
        }

        private void HandleError(string errorMessage, LocationFetchStatus status = LocationFetchStatus.Failed)
        {
            Status = status;
            CurrentLocation = LocationData.Invalid;
            OnLocationFetchFailed?.Invoke(errorMessage);
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

        /// <summary>
        /// Check if Unity Editor has Android as the selected build target
        /// </summary>
        private bool IsEditorWithAndroidTarget()
        {
            #if UNITY_EDITOR
                return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android;
            #else
                return false;
            #endif
        }

        /// <summary>
        /// Check if Unity Remote is connected (Editor only)
        /// </summary>
        private bool IsUnityRemoteConnected()
        {
            #if UNITY_EDITOR
                return UnityEditor.EditorApplication.isRemoteConnected;
            #else
                return false;
            #endif
        }

        /// <summary>
        /// Check if we should use mobile location (actual mobile, or Editor with Android target)
        /// </summary>
        public static bool ShouldUseMobileLocation()
        {
            #if UNITY_EDITOR
                // Use mobile provider if Android is the selected platform
                return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android ||
                       UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS;
            #elif UNITY_ANDROID || UNITY_IOS
                return true;
            #else
                return false;
            #endif
        }

        /// <summary>
        /// Open device app settings for manual permission granting
        /// </summary>
        public void OpenAppSettings()
        {
            _permissionHandler?.OpenAppSettings();
        }
    }
}