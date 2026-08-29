using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using WeatherAIAgent.Models;

namespace WeatherAgent.Services;

/// <summary>
/// Retrieves current weather information from WeatherAPI.
/// </summary>
public sealed class WeatherApiService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly WeatherApiOptions _options;

    public WeatherApiService(HttpClient httpClient, IOptions<WeatherApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<WeatherInfo> GetCurrentWeatherAsync(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location cannot be empty.", nameof(location));

        var aqi = _options.AirQuality ? "yes" : "no";
        var url =
            $"current.json?key={Uri.EscapeDataString(_options.ApiKey)}" +
            $"&q={Uri.EscapeDataString(location)}" +
            $"&lang={Uri.EscapeDataString(_options.Language)}" +
            $"&aqi={aqi}";

        try
        {
            using var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                var transient = IsTransientStatusCode(response.StatusCode);
                throw new WeatherServiceException(
                    $"Weather API request failed: {status} {response.ReasonPhrase}",
                    status,
                    transient);
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);
            return ParseWeather(json.RootElement);
        }
        catch (OperationCanceledException)
        {
            // Preserve cancellation so callers can distinguish it from a service failure.
            throw;
        }
        catch (WeatherServiceException)
        {
            throw;
        }
        catch (JsonException ex)
        {
            throw new WeatherServiceException(
                "Failed to parse weather service response.",
                innerException: ex);
        }
        catch (HttpRequestException ex)
        {
            throw new WeatherServiceException(
                "Weather service request failed.",
                isTransient: true,
                innerException: ex);
        }
    }

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

    private static bool IsTransientStatusCode(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

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
