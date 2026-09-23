using Microsoft.Extensions.Options;
using WeatherAgent.Models;
using WeatherAIAgent.Exceptions;
using WeatherAIAgent.Interfaces;

namespace WeatherAgent.Middleware;

/// <summary>
/// Verifies LLM provider health and applies the application-level agent timeout.
/// </summary>
public sealed class LLMHealthMiddleware : AgentMiddlewareBase<LLMHealthMiddleware>
{
    private readonly ILLMHealthService _healthService;
    private readonly IOptions<LLMOptions> _options;

    public override int Order => 15;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMHealthMiddleware"/> class.
    /// </summary>
    /// <param name="healthService">The LLM health service.</param>
    /// <param name="options">The LLM options.</param>
    /// <param name="logger">The logger.</param>
    public LLMHealthMiddleware(
        ILLMHealthService healthService,
        IOptions<LLMOptions> options,
        ILogger<LLMHealthMiddleware> logger)
        : base(logger)
    {
        this._healthService = healthService;
        this._options = options;
    }

    /// <summary>
    /// Invokes the middleware to check LLM health and apply timeout.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="AgentTimeoutException"></exception>
    public override async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var health = await _healthService.CheckAsync(context.CancellationToken);

        if (!health.IsHealthy)
        {
            Logger.LogWarning(
                "LLM health check failed. CorrelationId={CorrelationId}, Provider={Provider}, Model={Model}, EndpointAvailable={EndpointAvailable}, ModelAvailable={ModelAvailable}, Error={Error}",
                context.CorrelationId,
                health.Provider,
                health.Model,
                health.IsAvailable,
                health.IsModelAvailable,
                health.Error);

            throw new InvalidOperationException($"LLM provider is unavailable: {health.Error ?? "health check failed."}");
        }

        var timeoutSeconds = Math.Max(1, _options.Value.AgentTimeoutSeconds);
        context.Metadata.TimeoutSeconds = timeoutSeconds;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, timeoutCts.Token);

        var originalToken = context.CancellationToken;
        context.CancellationToken = linkedCts.Token;

        Logger.LogDebug(
            "LLM health check passed. CorrelationId={CorrelationId}, Provider={Provider}, Model={Model}, AgentTimeoutSeconds={TimeoutSeconds}",
            context.CorrelationId,
            health.Provider,
            health.Model,
            timeoutSeconds);

        try
        {
            return await next();
        }
        catch (OperationCanceledException)
            when (timeoutCts.IsCancellationRequested &&
                  !originalToken.IsCancellationRequested)
        {
            Logger.LogWarning(
                "Agent timeout reached. CorrelationId={CorrelationId}, Provider={Provider}, Model={Model}, TimeoutSeconds={TimeoutSeconds}",
                context.CorrelationId,
                health.Provider,
                health.Model,
                timeoutSeconds);

            throw new AgentTimeoutException($"The AI agent did not complete within {timeoutSeconds} seconds.");
        }
        finally
        {
            context.CancellationToken = originalToken;
        }
    }
}
