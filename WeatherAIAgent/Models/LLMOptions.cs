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
}