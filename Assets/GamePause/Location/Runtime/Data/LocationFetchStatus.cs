namespace GamePause.Location
{
    /// <summary>
    /// Status of location fetching operation
    /// </summary>
    public enum LocationFetchStatus
    {
        NotStarted,
        Initializing,
        Fetching,
        Success,
        Failed,
        PermissionDenied,
        Timeout,
        ServiceDisabled,
        RateLimited,
        LoadedFromCache
    }
}
