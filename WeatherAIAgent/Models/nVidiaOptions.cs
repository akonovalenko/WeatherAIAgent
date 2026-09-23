using WeatherAIAgent.Interfaces;

namespace WeatherAIAgent.Models;

/// <summary>
/// Represents the configuration options for the Nvidia service.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public sealed class NvidiaOptions : ILLMProviderOptions
{
    public const string SectionName = "nVidia";

    public string? ApiKey { get; set; }

    public string? Endpoint { get; set; }

    public string? Model { get; set; }

}