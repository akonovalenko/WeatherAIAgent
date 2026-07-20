/// <summary>
/// Represents the configuration options for the LLM service.   
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public sealed class LLMOptions
{
    public const string SectionName = "LLM";

    public string Provider { get; set; } = "nVidia";
}