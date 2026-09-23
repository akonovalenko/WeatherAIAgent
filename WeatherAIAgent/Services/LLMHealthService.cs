using System.Text.Json;
using Microsoft.Extensions.Options;
using WeatherAIAgent.Interfaces;
using WeatherAIAgent.Models;

namespace WeatherAgent.Services;

/// <summary>
/// Performs a cached health check against the configured OpenAI-compatible provider.
/// The /models endpoint is used to verify endpoint availability and configured model availability.
/// OpenAI-compatible APIs do not expose a universal "latest model version" contract,
/// so the configured model ID is treated as the expected current model.
/// </summary>
public sealed class LLMHealthService : ILLMHealthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<LLMOptions> _llmOptions;
    private readonly IOptions<OpenAIOptions> _openAiOptions;
    private readonly IOptions<NvidiaOptions> _nvidiaOptions;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<LLMHealthService> _logger;

    private LLMHealthResult? _cachedResult;
    private DateTimeOffset _cachedAt;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMHealthService"/> class with the specified dependencies.
    /// </summary>
    /// <param name="httpClientFactory">the HTTP client factory</param>
    /// <param name="llmOptions">the LLM options</param>
    /// <param name="openAiOptions">the OpenAI options</param>
    /// <param name="nvidiaOptions">the Nvidia options</param>
    /// <param name="logger">the logger</param>
    public LLMHealthService(
        IHttpClientFactory httpClientFactory,
        IOptions<LLMOptions> llmOptions,
        IOptions<OpenAIOptions> openAiOptions,
        IOptions<NvidiaOptions> nvidiaOptions,
        ILogger<LLMHealthService> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._llmOptions = llmOptions;
        this._openAiOptions = openAiOptions;
        this._nvidiaOptions = nvidiaOptions;
        this._logger = logger;
    }

    /// <summary>
    /// Checks the health of the configured LLM provider and model, returning a cached result if available and not expired.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The health result.</returns>
    public async Task<LLMHealthResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(0, _llmOptions.Value.HealthCheckIntervalSeconds));
        if (_cachedResult is not null && DateTimeOffset.UtcNow - this._cachedAt < interval)
            return _cachedResult;

        await this._gate.WaitAsync(cancellationToken);
        try
        {
            if (_cachedResult is not null && DateTimeOffset.UtcNow - this._cachedAt < interval)
                return this._cachedResult;

            var result = await CheckProviderAsync(cancellationToken);
            this._cachedResult = result;
            this._cachedAt = DateTimeOffset.UtcNow;
            return result;
        }
        finally
        {
            this._gate.Release();
        }
    }

    /// <summary>
    /// Checks the health of the configured LLM provider and model by sending a request to the provider's /models endpoint.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The health result.</returns>
    private async Task<LLMHealthResult> CheckProviderAsync(CancellationToken cancellationToken)
    {
        var provider = _llmOptions.Value.Provider?.Trim() ?? string.Empty;
        ILLMProviderOptions? options = provider.ToLowerInvariant() switch
        {
            "openai" => _openAiOptions.Value,
            "nvidia" => _nvidiaOptions.Value,
            _ => null
        };

        if (options is null)
            return new(false, false, provider, string.Empty, $"Unknown provider '{provider}'.");

        var apiKey = string.IsNullOrWhiteSpace(options.ApiKey)
            ? Environment.GetEnvironmentVariable("LLM_API_KEY")
            : options.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            return new(false, false, provider, options.Model ?? string.Empty, "API key is not configured.");

        if (string.IsNullOrWhiteSpace(options.Endpoint))
            return new(false, false, provider, options.Model ?? string.Empty, "Endpoint is not configured.");

        if (string.IsNullOrWhiteSpace(options.Model))
            return new(false, false, provider, string.Empty, "Model is not configured.");

        var timeoutSeconds = Math.Max(1, _llmOptions.Value.HealthCheckTimeoutSeconds);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{options.Endpoint.TrimEnd('/')}/models");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            _logger.LogDebug(
                "LLM health check started. Provider={Provider}, Model={Model}",
                provider,
                options.Model);

            using var response = await client.SendAsync(request, timeoutCts.Token);
            var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return new(false, false, provider, options.Model,
                    $"Provider returned {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            using var document = JsonDocument.Parse(body);
            var modelAvailable = document.RootElement.TryGetProperty("data", out var data) &&
                                 data.ValueKind == JsonValueKind.Array &&
                                 data.EnumerateArray().Any(model =>
                                     model.TryGetProperty("id", out var id) &&
                                     string.Equals(id.GetString(), options.Model, StringComparison.OrdinalIgnoreCase));

            return new(true, modelAvailable, provider, options.Model,
                modelAvailable ? null : $"Model '{options.Model}' is not available at the configured endpoint.");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return new(false, false, provider, options.Model, $"Health check timed out after {timeoutSeconds} seconds.");
        }
        catch (Exception ex)
        {
            return new(false, false, provider, options.Model, ex.Message);
        }
    }
}
