using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using WeatherAgent.Helpers;
using WeatherAgent.Models;
using WeatherAIAgent.Exceptions;
using WeatherAIAgent.Interfaces;
using WeatherAIAgent.Models;

namespace WeatherAgent.Tools;

/// <summary>
/// Represents a tool that retrieves the current weather for a specified location. This tool is designed to be called 
/// by an AI agent and will return authoritative weather information without modifying the returned text.
/// </summary>
public sealed class WeatherTool : IWeatherTool
{
    private const string ToolName = "GetCurrentWeather";
    private const int MaxAttempts = 3;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherTool> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherTool"/> class with the specified weather service and logger.
    /// </summary>
    /// <param name="weatherService">The weather service.</param>
    /// <param name="logger">The logger.</param>
    public WeatherTool(IWeatherService weatherService, ILogger<WeatherTool> logger)
    {
        this._weatherService = weatherService;
        this._logger = logger;
    }

    /// <summary>
    /// Creates an AI function that retrieves the current weather for a specified location. The function is designed to be called by an AI agent and will return authoritative weather information without modifying the returned text. 
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The AI function.</returns>
    public AIFunction Create(AgentContext context, CancellationToken cancellationToken) =>
        AIFunctionFactory.Create(
            async () => await ExecuteAsync(context, cancellationToken),
            ToolName,
            "Gets the current weather for the city explicitly entered by the user. The tool chooses the location from the user request; the model must not supply or reinterpret a city name. The returned text is authoritative and must not be modified.");

    /// <summary>
    /// Executes the weather tool by retrieving the current weather for the specified location and formatting the result.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The formatted weather information.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<string> ExecuteAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var location = context.Input.Trim();
        if (string.IsNullOrWhiteSpace(location))
            throw new InvalidOperationException("The requested weather location is empty.");

        context.Weather.RequestedLocation = location;
        context.Weather.ToolCallCount++;
        context.WeatherLocations.Add(location);

        var weather = await GetWeatherWithRetryAsync(location, cancellationToken);
        var formatted = FormatWeather(weather);

        context.Weather.WeatherInfo = weather;
        context.Weather.ResolvedLocation = weather.Location;
        context.Weather.FormattedWeather = formatted;
        return formatted;
    }

    /// <summary>
    /// Attempts to get the current weather for the specified location, retrying on transient errors up to a maximum number of attempts.
    /// </summary>
    /// <param name="location">The location for which to get weather information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The weather information for the specified location.</returns>
    /// <exception cref="WeatherServiceException"></exception>
    private async Task<WeatherInfo> GetWeatherWithRetryAsync(string location, CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await _weatherService.GetCurrentWeatherAsync(location, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (IsTransientWeatherError(ex) && attempt < MaxAttempts)
            {
                lastException = ex;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger.LogWarning(ex, "Transient weather error. Retrying in {DelayMs} ms. Attempt {Attempt}/{MaxAttempts}. Location={Location}", delay.TotalMilliseconds, attempt, MaxAttempts, location);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new WeatherServiceException($"Weather request failed after {MaxAttempts} attempts.", isTransient: false, innerException: lastException);
    }

    /// <summary>
    /// Determines whether the specified exception is a transient weather error that can be retried.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>true if the exception is a transient weather error; otherwise, false.</returns>
    private static bool IsTransientWeatherError(Exception ex) =>
        ex is WeatherServiceException { IsTransient: true } ||
        ex is HttpRequestException ||
        ex is TimeoutException ||
        (ex.InnerException is not null && IsTransientWeatherError(ex.InnerException));

    /// <summary>
    /// Formats the weather information into a human-readable string.
    /// </summary>
    /// <param name="weather">The weather information to format.</param>
    /// <returns>A human-readable string representing the weather information.</returns>
    private static string FormatWeather(WeatherInfo weather)
    {
        var airQuality = WeatherHelper.GetAirQualityDescription(weather.AirQualityIndex);
        return $"""
            Current weather information:

            Location:   {weather.Location}
            Region:     {weather.Region}
            Country:    {weather.Country}
            Latitude:   {weather.Latitude}
            Longitude:  {weather.Longitude}
            Timezone:   {weather.TimeZone}
            Local time: {weather.LocalTime}

            Temperature:
            - Current temperature: {weather.Temperature} °C
            - Feels like: {weather.FeelsLike} °C

            Weather condition:
            {weather.Condition}

            Atmospheric conditions:
            - Humidity: {weather.Humidity}%
            - Pressure: {weather.PressureMb} hPa
            - Cloud coverage: {weather.Cloud}%

            Wind conditions:
            - Wind speed: {weather.WindKph} km/h
            - Wind gust: {weather.GustKph} km/h
            - Wind direction: {weather.WindDirection}

            Visibility:
            - {weather.VisibilityKm} km

            Precipitation:
            - {weather.PrecipitationMm} mm

            UV information:
            - UV index: {weather.UvIndex}

            Air quality:
            - Status: {airQuality}
            - AQI: {weather.AirQualityIndex}
            - PM2.5: {weather.Pm25}
            - PM10: {weather.Pm10}
            """;
    }
}
