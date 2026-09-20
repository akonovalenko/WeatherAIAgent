using WeatherAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Assigns a correlation ID and logs the correlation context.
/// Lifecycle logging belongs to LoggingMiddleware.
/// </summary>
public sealed class CorrelationMiddleware
    : AgentMiddlewareBase<CorrelationMiddleware>
{
    public override int Order => 10;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    public CorrelationMiddleware(
        ILogger<CorrelationMiddleware> logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Assigns a correlation ID to the context if it is not already set.
    /// </summary>
    public override Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        if (string.IsNullOrWhiteSpace(context.CorrelationId))
        {
            context.CorrelationId = Guid.NewGuid().ToString("N");

            Logger.LogDebug(
                "Correlation ID assigned. CorrelationId={CorrelationId}",
                context.CorrelationId);
        }
        else
        {
            Logger.LogDebug(
                "Existing correlation ID preserved. CorrelationId={CorrelationId}",
                context.CorrelationId);
        }

        return next();
    }
}