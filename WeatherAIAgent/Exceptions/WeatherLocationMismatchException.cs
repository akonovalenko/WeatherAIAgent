namespace WeatherAIAgent.Exceptions;

/// <summary>
/// Indicates that the weather provider resolved a different location than the
/// location explicitly requested by the user.
/// </summary>
public sealed class WeatherLocationMismatchException : InvalidOperationException
{
    public string RequestedLocation { get; }
    public string ResolvedLocation { get; }
    
    public WeatherLocationMismatchException(string requestedLocation, string resolvedLocation)
        : base($"Weather location mismatch. Requested '{requestedLocation}', resolved '{resolvedLocation}'.")
    {
        RequestedLocation = requestedLocation;
        ResolvedLocation = resolvedLocation;
    }

}
