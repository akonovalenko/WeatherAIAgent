using WeatherAgent.Models;

namespace WeatherAIAgent.Models;

/// <summary>
/// Metadata collected during one agent execution.
/// </summary>
public sealed class AgentExecutionMetadata
{
    public int TimeoutSeconds { get; set; } = 120;

    public long DurationMs { get; set; }

    public int RetryAttempt { get; set; }

    public string? AgentError { get; set; }

    public TokenUsageInfo? TokenUsage { get; set; }
}
