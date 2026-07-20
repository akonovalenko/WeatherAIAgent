using WeatherAIAgent.Models;

namespace WeatherAgent.Services;

/// <summary>
/// Represents a service that provides weather information.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public interface IWeatherService
{
    /// <summary>
    /// Gets the current weather information for a specified location asynchronously.
    /// </summary>
    /// <param name="location">The location for which to retrieve weather information.</param>
    /// <returns>The current weather information.</returns>
    Task<WeatherInfo> GetCurrentWeatherAsync(string location);
}
