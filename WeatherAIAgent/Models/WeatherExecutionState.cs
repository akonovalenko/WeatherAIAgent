namespace WeatherAIAgent.Models;

/// <summary>
/// Represents the execution state of the weather agent, including requested and resolved locations, weather information, formatted weather output, and tool call count.
/// </summary>
public sealed class WeatherExecutionState
{
    public string? RequestedLocation { get; set; }
    public string? ResolvedLocation { get; set; }
    public WeatherInfo? WeatherInfo { get; set; }
    public string? FormattedWeather { get; set; }
    public int ToolCallCount { get; set; }
}
