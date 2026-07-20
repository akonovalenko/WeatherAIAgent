using Microsoft.Extensions.Options;
using System.Text.Json;
using WeatherAIAgent.Models;

namespace WeatherAgent.Services;

/// <summary>
/// Represents a service that retrieves weather information from a weather API.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public sealed class WeatherApiService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly WeatherApiOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherApiService"/> class with the specified HTTP client and weather API options.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for making API requests.</param>
    /// <param name="options">The weather API options.</param>
    public WeatherApiService(HttpClient httpClient, IOptions<WeatherApiOptions> options)
    {
        this._httpClient = httpClient;
        this._options = options.Value;
    }

    /// <summary>
    /// Retrieves the current weather information for the specified location asynchronously.
    /// </summary>
    /// <param name="location">The location for which to retrieve weather information.</param>
    /// <returns>A task that represents the asynchronous operation and returns the weather information.</returns>
    public async Task<WeatherInfo> GetCurrentWeatherAsync(string location)
    {
        try
        {
            var aqi = this._options.AirQuality 
                ? "yes" 
                : "no";

            var url =
                $"{this._options.BaseUrl}/current.json" +
                $"?key={this._options.ApiKey}" +
                $"&q={Uri.EscapeDataString(location)}" +
                $"&lang={this._options.Language}" +
                $"&aqi={aqi}";

            using var response = await this._httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Throw a concise, structured exception (contains Status) for upstream concise logging.
                throw new WeatherServiceException(
                    $"Weather API request failed: {(int)response.StatusCode} {response.ReasonPhrase}",
                    status: (int)response.StatusCode);
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);
            var root = json.RootElement;

            return new WeatherInfo
            {
                Location = root.GetProperty("location").GetProperty("name").GetString()!,
                Region = root.GetProperty("location").GetProperty("region").GetString() ?? "Unknown",
                Country = root.GetProperty("location").GetProperty("country").GetString() ?? "Unknown",
                Latitude = root.GetProperty("location").GetProperty("lat").GetDouble(),
                Longitude = root.GetProperty("location").GetProperty("lon").GetDouble(),
                TimeZone = root.GetProperty("location").GetProperty("tz_id").GetString() ?? "Unknown",
                LocalTime = root.GetProperty("location").GetProperty("localtime").GetString() ?? "Unknown",

                Temperature = root.GetProperty("current").GetProperty("temp_c").GetDouble(),
                FeelsLike = root.GetProperty("current").GetProperty("feelslike_c").GetDouble(),
                Humidity = root.GetProperty("current").GetProperty("humidity").GetInt32(),
                WindKph = root.GetProperty("current").GetProperty("wind_kph").GetDouble(),
                GustKph = root.GetProperty("current").GetProperty("gust_kph").GetDouble(),
                WindDirection = root.GetProperty("current").GetProperty("wind_dir").GetString()!,
                Condition = root.GetProperty("current").GetProperty("condition").GetProperty("text").GetString()!,
                PressureMb = root.GetProperty("current").GetProperty("pressure_mb").GetDouble(),
                Cloud = root.GetProperty("current").GetProperty("cloud").GetInt32(),
                VisibilityKm = root.GetProperty("current").GetProperty("vis_km").GetDouble(),
                UvIndex = root.GetProperty("current").GetProperty("uv").GetDouble(),
                PrecipitationMm = root.GetProperty("current").GetProperty("precip_mm").GetDouble(),
                AirQualityIndex = GetAirQualityIndex(root),
                Pm25 = GetAirQualityValue(root, "pm2_5"),
                Pm10 = GetAirQualityValue(root, "pm10"),
            };
        }
        catch (JsonException jex)
        {
            // JSON parsing error -> concise domain exception
            throw new WeatherServiceException($"Failed to parse weather response: {jex.Message}", innerException: jex);
        }
        catch (WeatherServiceException)
        {
            // rethrow concise exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            // Fallback: wrap any unexpected error into a concise domain exception
            throw new WeatherServiceException($"Weather service failure: {ex.Message}", innerException: ex);
        }
    }

    /// <summary>
    /// Gets the air quality value for the specified property name from the JSON root element.
    /// </summary>
    /// <param name="root">The JSON root element.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The air quality value.</returns>
    private static double GetAirQualityValue(JsonElement root, string name)
    {
        if (!root.TryGetProperty("current", out var current))
            return 0;

        if (!current.TryGetProperty("air_quality", out var airQuality))
            return 0;

        if (!airQuality.TryGetProperty(name, out var value))
            return 0;

        return value.GetDouble();
    }

    /// <summary>
    /// Gets the air quality index from the JSON root element.
    /// </summary>
    /// <param name="root">The JSON root element.</param>
    /// <returns>The air quality index.</returns>
    private static int GetAirQualityIndex(JsonElement root)
    {
        if (!root.TryGetProperty("current", out var current))
            return 0;

        if (!current.TryGetProperty("air_quality", out var airQuality))
            return 0;

        if (!airQuality.TryGetProperty("us-epa-index", out var index))
            return 0;

        return index.GetInt32();
    }


}