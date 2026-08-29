namespace WeatherAIAgent.Models;

/// <summary>
/// Per-request state shared by the agent pipeline.
/// </summary>
public sealed class AgentContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public string Input { get; set; } = string.Empty;
    public string UserId { get; set; } = "console-user";
    public Dictionary<string, object> Items { get; } = new();

    /// <summary>
    /// All locations used by weather tool calls during this request.
    /// </summary>
    public HashSet<string> WeatherLocations { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? WeatherLocation
    {
        get => Items.TryGetValue("WeatherLocation", out var value) ? value?.ToString() : null;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
                Items["WeatherLocation"] = value;
        }
    }
}
