using System;
using UnityEngine;

namespace GamePause.Location
{
    /// <summary>
    /// Settings for location persistence and rate limiting
    /// </summary>
    [Serializable]
    public class LocationPersistenceSettings
    {
        [Tooltip("Enable location data persistence across sessions")]
        public bool EnablePersistence = true;

        [Tooltip("Enable rate limiting for location updates")]
        public bool EnableRateLimiting = false;

        [Tooltip("Minimum hours between automatic location updates")]
        [Range(1, 48)]
        public int RateLimitHours = 12;

        [Tooltip("Allow bypassing rate limit for manual updates")]
        public bool AllowManualBypass = true;
    }
}
