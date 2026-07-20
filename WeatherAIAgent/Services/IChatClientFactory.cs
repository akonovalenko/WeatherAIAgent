using OpenAI.Chat;

namespace WeatherAgent.Services;

/// <summary>
/// Represents a factory for creating instances of the ChatClient class.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public interface IChatClientFactory
{
    /// <summary>
    /// Creates a new instance of the ChatClient class.
    /// </summary>
    /// <returns>The created ChatClient instance.</returns>
    ChatClient Create();
}