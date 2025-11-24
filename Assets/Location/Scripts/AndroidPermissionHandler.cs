using System;
using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace GamePause.Location
{
    /// <summary>
    /// Handles Android runtime permission requests for location services
    /// </summary>
    public class AndroidPermissionHandler
    {
        private readonly MonoBehaviour _coroutineRunner;
        private const float PERMISSION_CHECK_INTERVAL = 0.5f;
        private const float PERMISSION_TIMEOUT = 30f;

        public event Action OnPermissionGranted;
        public event Action OnPermissionDenied;

        public AndroidPermissionHandler(MonoBehaviour coroutineRunner)
        {
            _coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
        }

        /// <summary>
        /// Check if location permission is granted
        /// </summary>
        public bool HasLocationPermission()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
                return Permission.HasUserAuthorizedPermission(Permission.FineLocation);
            #else
                return true;
            #endif
        }

        /// <summary>
        /// Request location permission and wait for result
        /// </summary>
        public void RequestLocationPermission()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
                if (HasLocationPermission())
                {
                    OnPermissionGranted?.Invoke();
                    return;
                }

                _coroutineRunner.StartCoroutine(RequestPermissionCoroutine());
            #else
                OnPermissionGranted?.Invoke();
            #endif
        }

        #if UNITY_ANDROID && !UNITY_EDITOR
        private IEnumerator RequestPermissionCoroutine()
        {
            // Request the permission
            Permission.RequestUserPermission(Permission.FineLocation);

            // Wait a frame for the dialog to appear
            yield return new WaitForSeconds(0.1f);

            // Wait for user response with timeout
            float elapsedTime = 0f;
            bool wasInBackground = false;

            while (elapsedTime < PERMISSION_TIMEOUT)
            {
                // Check if app went to background (permission dialog is showing)
                if (!Application.isFocused)
                {
                    wasInBackground = true;
                }

                // If app came back to foreground after being in background, check permission
                if (wasInBackground && Application.isFocused)
                {
                    yield return new WaitForSeconds(0.1f); // Small delay to ensure permission state is updated
                    
                    if (HasLocationPermission())
                    {
                        OnPermissionGranted?.Invoke();
                    }
                    else
                    {
                        OnPermissionDenied?.Invoke();
                    }
                    yield break;
                }

                yield return new WaitForSeconds(PERMISSION_CHECK_INTERVAL);
                elapsedTime += PERMISSION_CHECK_INTERVAL;

                // Also check if permission was granted while waiting
                if (HasLocationPermission())
                {
                    OnPermissionGranted?.Invoke();
                    yield break;
                }
            }

            // Timeout - check final state
            if (HasLocationPermission())
            {
                OnPermissionGranted?.Invoke();
            }
            else
            {
                OnPermissionDenied?.Invoke();
            }
        }
        #endif

        /// <summary>
        /// Open app settings so user can manually enable permissions
        /// </summary>
        public void OpenAppSettings()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var currentActivityObject = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var settingsIntent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS"))
                    using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                    using (var uri = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", Application.identifier, null))
                    {
                        settingsIntent.Call<AndroidJavaObject>("setData", uri);
                        currentActivityObject.Call("startActivity", settingsIntent);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AndroidPermissionHandler] Failed to open app settings: {e.Message}");
                }
            #endif
        }
    }
}