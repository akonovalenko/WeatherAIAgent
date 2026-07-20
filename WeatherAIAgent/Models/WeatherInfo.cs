namespace WeatherAIAgent.Models;

/// <summary>
/// Represents detailed weather information for a specific location.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public sealed class WeatherInfo
{
    /// <summary>
    /// City or location name.
    /// </summary>
    public required string Location { get; set; } = "";

    /// <summary>
    /// Current temperature in Celsius.
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Feels like temperature in Celsius.
    /// </summary>
    public double FeelsLike { get; set; }

    /// <summary>
    /// Relative humidity percentage.
    /// </summary>
    public int Humidity { get; set; }

    /// <summary>
    /// Wind speed in kilometers per hour.
    /// </summary>
    public double WindKph { get; set; }

    /// <summary>
    /// Maximum wind gust speed in kilometers per hour.
    /// </summary>
    public double GustKph { get; set; }

    /// <summary>
    /// Wind direction (N, NE, E, etc.).
    /// </summary>
    public string WindDirection { get; set; } = "";

    /// <summary>
    /// Weather condition description.
    /// </summary>
    public string Condition { get; set; } = "";

    /// <summary>
    /// Atmospheric pressure in millibars.
    /// </summary>
    public double PressureMb { get; set; }

    /// <summary>
    /// Cloud coverage percentage.
    /// </summary>
    public int Cloud { get; set; }

    /// <summary>
    /// Visibility distance in kilometers.
    /// </summary>
    public double VisibilityKm { get; set; }

    /// <summary>
    /// UV index.
    /// </summary>
    public double UvIndex { get; set; }

    /// <summary>
    /// Amount of precipitation in millimeters.
    /// </summary>
    public double PrecipitationMm { get; set; }

    /// <summary>
    /// Air quality index.
    /// </summary>
    public int AirQualityIndex { get; set; }

    /// <summary>
    /// PM2.5 particle concentration.
    /// </summary>
    public double Pm25 { get; set; }

    /// <summary>
    /// PM10 particle concentration.
    /// </summary>
    public double Pm10 { get; set; }

    /// <summary>
    /// Region or state of the location.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Country of the location.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Latitude coordinate of the location.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate of the location.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Time zone of the location.
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// Local time at the location.
    /// </summary>
    public string? LocalTime { get; set; }

}