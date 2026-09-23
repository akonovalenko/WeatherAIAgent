using System.Diagnostics;
using WeatherAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Logs request lifecycle events. It does not create correlation IDs.
/// </summary>
public sealed class LoggingMiddleware : AgentMiddlewareBase<LoggingMiddleware>
{
    public override int Order => 30;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger"></param>
    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Invokes the middleware logic, logging the start and end of the request, as well as any exceptions that occur.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public override async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var stopwatch = Stopwatch.StartNew();

        this.Logger.LogInformation($"Agent request started. CorrelationId={context.CorrelationId}, InputLength={context.Input.Length}");

        try
        {
            var result = await next();

            this.Logger.LogInformation($"Agent request completed. CorrelationId={context.CorrelationId}, DurationMs={stopwatch.ElapsedMilliseconds}");

            return result;
        }
        catch (Exception ex)
        {
            context.Metadata.AgentError = ex.Message;

            this.Logger.LogError(
                ex,
                "Agent request failed. CorrelationId={CorrelationId}, DurationMs={DurationMs}",
                context.CorrelationId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
