using Microsoft.Extensions.Logging;
using WeatherAIAgent.Helpers;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Logs request lifecycle events without logging the complete user prompt.
/// </summary>
public sealed class LoggingMiddleware : IAgentMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware, logging the start and end of the request lifecycle, as well as any exceptions that occur.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var startedAt = DateTimeOffset.UtcNow;

        this._logger.LogInformation(
            "Agent request started. CorrelationId={CorrelationId}, InputLength={InputLength}",
            context.CorrelationId,
            context.Input.Length);

        try
        {
            var result = await next();
            this._logger.LogInformation(
                "Agent request completed. CorrelationId={CorrelationId}, ElapsedMs={ElapsedMs}",
                context.CorrelationId,
                (DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Agent request failed. CorrelationId={CorrelationId}, Error={Error}",
                context.CorrelationId,
                ErrorHelper.GetShortError(ex));
            throw;
        }
    }
}
