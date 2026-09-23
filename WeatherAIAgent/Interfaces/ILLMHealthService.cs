using WeatherAIAgent.Models;

namespace WeatherAIAgent.Interfaces;

/// <summary>
/// Checks the configured LLM provider endpoint and model availability.
/// </summary>
public interface ILLMHealthService
{
    /// <summary>
    /// Checks the configured LLM provider endpoint and model availability.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The health result.</returns>
    Task<LLMHealthResult> CheckAsync(CancellationToken cancellationToken = default);
}
