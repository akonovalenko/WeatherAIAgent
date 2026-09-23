using WeatherAIAgent.Interfaces;

namespace WeatherAIAgent.Models;

/// <summary>
/// Represents the configuration options for the OpenAI service.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public sealed class OpenAIOptions :  ILLMProviderOptions
{
    public const string SectionName = "OpenAI";

    public string? ApiKey { get; set; }

    public string? Endpoint { get; set; }

    public string? Model { get; set; }

}