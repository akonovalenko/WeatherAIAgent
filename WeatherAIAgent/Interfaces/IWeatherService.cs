using WeatherAIAgent.Models;

namespace WeatherAIAgent.Interfaces;

/// <summary>
/// Represents a service that provides weather information.
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Gets the current weather information for a specified location.
    /// </summary>
    /// <param name="location">The location for which to retrieve weather information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current weather information.</returns>
    Task<WeatherInfo> GetCurrentWeatherAsync(
        string location,
        CancellationToken cancellationToken = default);
}
