namespace WeatherAIAgent.Models;

/// <summary>
/// Represents the context of an agent, including correlation ID, input, user ID, and additional items.
/// </summary>
public sealed class AgentContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string Input { get; set; } = "";
    public string UserId { get; set; } = "console-user";
    public Dictionary<string, object> Items { get; } = new();
    public string? WeatherLocation
    {
        get =>  Items.TryGetValue("WeatherLocation", out var value) ? value?.ToString() : null;
        set { if (value != null) Items["WeatherLocation"] = value;  }
    }

}