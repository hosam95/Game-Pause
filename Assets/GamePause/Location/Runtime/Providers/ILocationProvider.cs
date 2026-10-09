using System;

namespace GamePause.Location
{
    /// <summary>
    /// Interface for location providers
    /// </summary>
    public interface ILocationProvider
    {
        LocationFetchStatus Status { get; }
        LocationData CurrentLocation { get; }
        bool IsLocationAvailable { get; }
        void StartFetchingLocation();
        void StopFetchingLocation();
        event Action<LocationData> OnLocationFetched;
        event Action<string> OnLocationFetchFailed;
    }
}
