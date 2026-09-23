using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using WeatherAIAgent.Exceptions;
using WeatherAIAgent.Interfaces;
using WeatherAIAgent.Models;

namespace WeatherAgent.Services;

/// <summary>
/// Retrieves current weather information from WeatherAPI.
/// </summary>
public sealed class WeatherApiService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly WeatherApiOptions _options;
    private readonly ILogger<WeatherApiService> _logger;

    /// <summary>
    /// Initializes a new instance of the WeatherApiService class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="options">The weather API options.</param>
    public WeatherApiService(
        HttpClient httpClient,
        IOptions<WeatherApiOptions> options,
        ILogger<WeatherApiService> logger)
    {
        this._httpClient = httpClient;
        this._options = options.Value;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the current weather information for the specified location.
    /// </summary>
    /// <param name="location">The location for which to retrieve weather information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current weather information.</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="WeatherServiceException"></exception>
    public async Task<WeatherInfo> GetCurrentWeatherAsync(
        string location,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location cannot be empty.", nameof(location));

        var aqi = this._options.AirQuality ? "yes" : "no";
        var url =
            $"current.json?key={Uri.EscapeDataString(this._options.ApiKey)}" +
            $"&q={Uri.EscapeDataString(location.Trim())}" +
            $"&lang={Uri.EscapeDataString(this._options.Language)}" +
            $"&aqi={aqi}";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        this._logger.LogInformation(
            "Weather API request started. Location={Location}, AirQuality={AirQuality}, Language={Language}",
            location,
            this._options.AirQuality,
            this._options.Language);

        try
        {
            using var response = await this._httpClient.GetAsync(url, cancellationToken);

            this._logger.LogInformation(
                "Weather API HTTP response received. Location={Location}, StatusCode={StatusCode}, DurationMs={DurationMs}",
                location,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                throw new WeatherServiceException(
                    $"Weather API request failed: {status} {response.ReasonPhrase}",
                    status,
                    IsTransientStatusCode(response.StatusCode));
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var weather = ParseWeather(json.RootElement);
            stopwatch.Stop();

            this._logger.LogInformation(
                "Weather API request completed. RequestedLocation={RequestedLocation}, ResolvedLocation={ResolvedLocation}, DurationMs={DurationMs}",
                location,
                weather.Location,
                stopwatch.ElapsedMilliseconds);

            return weather;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            this._logger.LogWarning(
                "Weather API request cancelled. Location={Location}, DurationMs={DurationMs}",
                location,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (WeatherServiceException ex)
        {
            stopwatch.Stop();
            this._logger.LogWarning(
                ex,
                "Weather API request failed. Location={Location}, DurationMs={DurationMs}",
                location,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            this._logger.LogError(
                ex,
                "Weather API response parsing failed. Location={Location}, DurationMs={DurationMs}",
                location,
                stopwatch.ElapsedMilliseconds);
            throw new WeatherServiceException(
                "Failed to parse weather service response.",
                innerException: ex);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            this._logger.LogError(
                ex,
                "Weather API HTTP request failed. Location={Location}, DurationMs={DurationMs}",
                location,
                stopwatch.ElapsedMilliseconds);
            throw new WeatherServiceException(
                "Weather service request failed.",
                isTransient: true,
                innerException: ex);
        }
    }

    /// <summary>
    /// Parses the JSON response from the weather API into a WeatherInfo object.
    /// </summary>
    /// <param name="root">The root JSON element.</param>
    /// <returns>The parsed weather information.</returns>
    private static WeatherInfo ParseWeather(JsonElement root)
    {
        var location = root.GetProperty("location");
        var current = root.GetProperty("current");
        var condition = current.GetProperty("condition");

        return new WeatherInfo
        {
            Location = location.GetProperty("name").GetString() ?? "Unknown",
            Region = location.GetProperty("region").GetString() ?? "Unknown",
            Country = location.GetProperty("country").GetString() ?? "Unknown",
            Latitude = location.GetProperty("lat").GetDouble(),
            Longitude = location.GetProperty("lon").GetDouble(),
            TimeZone = location.GetProperty("tz_id").GetString() ?? "Unknown",
            LocalTime = location.GetProperty("localtime").GetString() ?? "Unknown",
            Temperature = current.GetProperty("temp_c").GetDouble(),
            FeelsLike = current.GetProperty("feelslike_c").GetDouble(),
            Humidity = current.GetProperty("humidity").GetInt32(),
            WindKph = current.GetProperty("wind_kph").GetDouble(),
            GustKph = current.GetProperty("gust_kph").GetDouble(),
            WindDirection = current.GetProperty("wind_dir").GetString() ?? "Unknown",
            Condition = condition.GetProperty("text").GetString() ?? "Unknown",
            PressureMb = current.GetProperty("pressure_mb").GetDouble(),
            Cloud = current.GetProperty("cloud").GetInt32(),
            VisibilityKm = current.GetProperty("vis_km").GetDouble(),
            UvIndex = current.GetProperty("uv").GetDouble(),
            PrecipitationMm = current.GetProperty("precip_mm").GetDouble(),
            AirQualityIndex = GetAirQualityIndex(root),
            Pm25 = GetAirQualityValue(root, "pm2_5"),
            Pm10 = GetAirQualityValue(root, "pm10")
        };
    }

    /// <summary>
    /// Determines whether the specified HTTP status code is considered transient.  
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>true if the status code is transient; otherwise, false.</returns>
    private static bool IsTransientStatusCode(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    /// <summary>
    /// Gets the air quality value for a specific pollutant from the JSON response.
    /// </summary>
    /// <param name="root">The root JSON element.</param>
    /// <param name="name">The name of the pollutant.</param>
    /// <returns>The air quality value.</returns>
    private static double GetAirQualityValue(JsonElement root, string name)
    {
        if (!root.TryGetProperty("current", out var current) ||
            !current.TryGetProperty("air_quality", out var airQuality) ||
            !airQuality.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.Number)
        {
            return 0;
        }

        return value.GetDouble();
    }

    /// <summary>
    /// Gets the US EPA Air Quality Index (AQI) from the JSON response.
    /// </summary>
    /// <param name="root">The root JSON element.</param>
    /// <returns>The AQI value.</returns>
    private static int GetAirQualityIndex(JsonElement root)
    {
        if (!root.TryGetProperty("current", out var current) ||
            !current.TryGetProperty("air_quality", out var airQuality) ||
            !airQuality.TryGetProperty("us-epa-index", out var index) ||
            index.ValueKind != JsonValueKind.Number)
        {
            return 0;
        }

        return index.GetInt32();
    }
}
