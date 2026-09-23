namespace WeatherAIAgent.Models;

/// <summary>
/// Result of an LLM provider health and configured model availability check.
/// </summary>
public sealed record LLMHealthResult(
    bool IsAvailable,
    bool IsModelAvailable,
    string Provider,
    string Model,
    string? Error = null)
{
    public bool IsHealthy => IsAvailable && IsModelAvailable;
}
