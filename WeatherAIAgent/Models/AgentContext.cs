using WeatherAIAgent.Models;

namespace WeatherAgent.Models;

/// <summary>
/// Represents the context of an agent execution, including input, user information, cancellation token, and execution metadata.
/// </summary>
public sealed class AgentContext
{
    public string CorrelationId { get; set; } = string.Empty;

    public string Input { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public CancellationToken CancellationToken { get; set; }

    public WeatherExecutionState Weather { get; } = new();

    public HashSet<string> WeatherLocations { get; } = new(StringComparer.OrdinalIgnoreCase);

    public AgentExecutionMetadata Metadata { get; } = new();
}
