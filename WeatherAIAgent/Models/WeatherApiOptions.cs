/// <summary>
/// Represents the configuration options for the Weather API service.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public sealed class WeatherApiOptions
{
    public const string SectionName = "WeatherApi";

    public required string ApiKey { get; init; }

    public required string BaseUrl { get; init; }
    public required string Language { get; init; }

    public bool AirQuality { get; init; } = false;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}
