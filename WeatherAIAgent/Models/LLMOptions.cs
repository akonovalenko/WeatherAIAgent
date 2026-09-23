/// <summary>
/// Represents the configuration options for the LLM service.   
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public sealed class LLMOptions
{
    public const string SectionName = "LLM";

    public string Provider { get; set; } = "nVidia";

    /// <summary>Maximum time allowed for a single AIAgent run.</summary>
    public int AgentTimeoutSeconds { get; set; } = 120;

    /// <summary>How often the provider health state is refreshed.</summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>Maximum time allowed for a provider health check.</summary>
    public int HealthCheckTimeoutSeconds { get; set; } = 5;
}