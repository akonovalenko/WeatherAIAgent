namespace WeatherAIAgent.Models;

/// <summary>
/// Represents the configuration options for the Nvidia service.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public sealed class NvidiaOptions : ILLMProviderOptions

{
    public const string SectionName = "nVidia";

    public string ApiKey { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;
}