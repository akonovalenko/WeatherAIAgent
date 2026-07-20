using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using WeatherAIAgent.Models;

namespace WeatherAgent.Services;

/// <summary>
/// Factory class for creating instances of <see cref="ChatClient"/> based on the configured LLM provider options.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public sealed class ChatClientFactory : IChatClientFactory
{
    private readonly ILLMProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatClientFactory"/> class with the specified LLM provider options.
    /// </summary>
    /// <param name="llmOptions">The LLM provider options.</param>
    /// <param name="openAiOptions">The OpenAI provider options.</param>
    /// <param name="nvidiaOptions">The NVIDIA provider options.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public ChatClientFactory(
        IOptions<LLMOptions> llmOptions,
        IOptions<OpenAIOptions> openAiOptions,
        IOptions<NvidiaOptions> nvidiaOptions)
    {
        this._options = llmOptions.Value.Provider.ToLowerInvariant() switch
        {
            "openai" => openAiOptions.Value,
            "nvidia" => nvidiaOptions.Value,
            _ => throw new InvalidOperationException($"Unknown provider '{llmOptions.Value.Provider}'.")
        };
    }

    /// <summary>
    /// Creates a new instance of <see cref="ChatClient"/> based on the configured LLM provider options.
    /// </summary>
    /// <returns>The created <see cref="ChatClient"/> instance.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public ChatClient Create()
    {
        var apiKey = string.IsNullOrWhiteSpace(this._options.ApiKey)
            ? Environment.GetEnvironmentVariable("LLM_API_KEY")
            : this._options.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("API key is not configured.");

        if (string.IsNullOrWhiteSpace(this._options.Endpoint))
            throw new InvalidOperationException("Endpoint is not configured.");

        if (string.IsNullOrWhiteSpace(this._options.Model))
            throw new InvalidOperationException("Model is not configured.");

        return new ChatClient(
            model: this._options.Model,
            credential: new ApiKeyCredential(apiKey),
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri(this._options.Endpoint)
            });
    }
}