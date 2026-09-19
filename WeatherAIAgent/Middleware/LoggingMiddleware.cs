using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WeatherAgent.Models;
using WeatherAIAgent.Interfaces;

namespace WeatherAgent.Middleware;

/// <summary>
/// Logs request lifecycle events. It does not create correlation IDs.
/// </summary>
public sealed class LoggingMiddleware : IAgentMiddleware
{
    public int Order => 30;

    private readonly ILogger<LoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger"></param>
    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware logic, logging the start and end of the request, as well as any exceptions that occur.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var stopwatch = Stopwatch.StartNew();

        this._logger.LogInformation(
            "Agent request started. CorrelationId={CorrelationId}, InputLength={InputLength}",
            context.CorrelationId,
            context.Input.Length);

        try
        {
            var result = await next();

            this._logger.LogInformation(
                "Agent request completed. CorrelationId={CorrelationId}, DurationMs={DurationMs}",
                context.CorrelationId,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            context.Metadata.AgentError = ex.Message;

            this._logger.LogError(
                ex,
                "Agent request failed. CorrelationId={CorrelationId}, DurationMs={DurationMs}",
                context.CorrelationId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
