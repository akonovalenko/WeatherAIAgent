namespace WeatherAIAgent.Interfaces;

/// <summary>
/// Represents the configuration options for a large language model (LLM) provider.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public interface ILLMProviderOptions
{
    string ApiKey { get; }
    string Endpoint { get; }
    string Model { get; }
    int TimeOutSec { get; }
}