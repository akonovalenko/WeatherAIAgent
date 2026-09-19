using WeatherAgent.Models;
using WeatherAIAgent.Exceptions;
using WeatherAIAgent.Interfaces;

namespace WeatherAgent.Middleware;

/// <summary>
/// Validates that the final weather response is authoritative and corresponds
/// to the city explicitly entered by the user.
/// </summary>
public sealed class OutputValidationMiddleware : IAgentMiddleware
{
    public int Order => 90;

    /// <summary>
    /// Validates that the final weather response is authoritative and corresponds
    /// to the city explicitly entered by the user.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="WeatherLocationMismatchException"></exception>
    public async Task<string> InvokeAsync(AgentContext context, Func<Task<string>> next)
    {
        var output = await next();
        var expectedWeather = context.Weather.FormattedWeather;

        if (string.IsNullOrWhiteSpace(expectedWeather))
        {
            if (context.WeatherLocations.Count > 0)
            {
                throw new InvalidOperationException(
                    "Weather tool execution did not produce an authoritative response.");
            }

            return output;
        }

        var requestedLocation = context.Weather.RequestedLocation;
        var weather = context.Weather.WeatherInfo;
        if (string.IsNullOrWhiteSpace(requestedLocation) || weather is null)
            throw new InvalidOperationException("Output validation failed: authoritative weather data is missing.");

        if (!LocationsMatch(requestedLocation, weather.Location))
            throw new WeatherLocationMismatchException(requestedLocation, weather.Location);

        if (!string.Equals(output, expectedWeather, StringComparison.Ordinal))
            throw new InvalidOperationException("Output validation failed: the response was modified after weather data was retrieved.");

        return output;
    }

    /// <summary>
    /// Determines whether the requested location matches the resolved location from the weather service, ignoring case and whitespace. 
    /// </summary>
    /// <param name="requested">The requested location.</param>
    /// <param name="resolved">The resolved location.</param>
    /// <returns>true if the locations match; otherwise, false.</returns>
    private static bool LocationsMatch(string requested, string resolved)
    {
        var requestedNormalized = NormalizeLocation(requested);
        var resolvedNormalized = NormalizeLocation(resolved);

        return string.Equals(
            requestedNormalized,
            resolvedNormalized,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes a location string by trimming whitespace and extracting the first comma-separated component, if present. This helps to compare city names in a consistent manner.    
    /// </summary>
    /// <param name="value">The location string to normalize.</param>
    /// <returns>The normalized location string.</returns>
    private static string NormalizeLocation(string value)
    {
        var normalized = value.Trim();

        // WeatherAPI accepts values such as "Dnipro, Ukraine" but normally
        // returns the canonical city in the Location field. Compare the first
        // comma-separated city component in that common case.
        var commaIndex = normalized.IndexOf(',');
        if (commaIndex > 0)
            normalized = normalized[..commaIndex];

        return normalized.Trim();
    }
}
