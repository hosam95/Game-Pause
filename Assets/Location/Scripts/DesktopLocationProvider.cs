using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GamePause.Location
{
    /// <summary>
    /// Location provider that uses IP geolocation for desktop platforms
    /// </summary>
    public class DesktopLocationProvider : ILocationProvider
    {
        private const string IP_API_URL = "http://ip-api.com/json/?fields=status,message,country,city,lat,lon,timezone";
        private const int REQUEST_TIMEOUT = 15;

        public LocationFetchStatus Status { get; private set; } = LocationFetchStatus.NotStarted;
        public LocationData CurrentLocation { get; private set; } = LocationData.Invalid;
        public bool IsLocationAvailable => Status == LocationFetchStatus.Success && CurrentLocation.IsValid;

        public event Action<LocationData> OnLocationFetched;
        public event Action<string> OnLocationFetchFailed;

        private readonly MonoBehaviour _coroutineRunner;
        private Coroutine _fetchCoroutine;

        public DesktopLocationProvider(MonoBehaviour coroutineRunner)
        {
            _coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
        }

        public void StartFetchingLocation()
        {
            if (Status == LocationFetchStatus.Fetching)
            {
                return;
            }

            Status = LocationFetchStatus.Fetching;
            _fetchCoroutine = _coroutineRunner.StartCoroutine(FetchLocationFromIPCoroutine());
        }

        public void StopFetchingLocation()
        {
            if (_fetchCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_fetchCoroutine);
                _fetchCoroutine = null;
            }

            if (Status == LocationFetchStatus.Fetching)
            {
                Status = LocationFetchStatus.NotStarted;
            }
        }

        private IEnumerator FetchLocationFromIPCoroutine()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(IP_API_URL))
            {
                request.timeout = REQUEST_TIMEOUT;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ProcessResponse(request.downloadHandler.text);
                }
                else
                {
                    HandleError($"Network request failed: {request.error}");
                }
            }

            _fetchCoroutine = null;
        }

        private void ProcessResponse(string jsonResponse)
        {
            try
            {
                IPApiResponse response = JsonUtility.FromJson<IPApiResponse>(jsonResponse);

                if (response.status == "success")
                {
                    CurrentLocation = new LocationData(
                        response.lat,
                        response.lon,
                        response.city,
                        response.country,
                        response.timezone,
                        false
                    );

                    Status = LocationFetchStatus.Success;
                    OnLocationFetched?.Invoke(CurrentLocation);
                }
                else
                {
                    HandleError($"API returned error: {response.message}");
                }
            }
            catch (Exception ex)
            {
                HandleError($"Failed to parse response: {ex.Message}");
            }
        }

        private void HandleError(string errorMessage)
        {
            Status = LocationFetchStatus.Failed;
            CurrentLocation = LocationData.Invalid;
            OnLocationFetchFailed?.Invoke(errorMessage);
        }

        [Serializable]
        private class IPApiResponse
        {
            public string status;
            public string message;
            public string country;
            public string city;
            public float lat;
            public float lon;
            public string timezone;
        }
    }
}