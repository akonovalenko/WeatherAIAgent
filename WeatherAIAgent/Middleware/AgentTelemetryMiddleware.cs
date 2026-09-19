using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WeatherAgent.Models;
using WeatherAIAgent.Interfaces;

namespace WeatherAgent.Middleware;

/// <summary>
/// Measures the complete request duration.
/// </summary>
public sealed class AgentTelemetryMiddleware : IAgentMiddleware
{
    public int Order => 20;

    private readonly ILogger<AgentTelemetryMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentTelemetryMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use for logging telemetry information.</param>
    public AgentTelemetryMiddleware(
        ILogger<AgentTelemetryMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to measure the duration of the request and log telemetry information.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await next();
        }
        finally
        {
            stopwatch.Stop();
            context.Metadata.DurationMs = stopwatch.ElapsedMilliseconds;

            this._logger.LogInformation(
                "Agent metrics: CorrelationId={CorrelationId}, DurationMs={DurationMs}, InputLength={InputLength}",
                context.CorrelationId,
                stopwatch.ElapsedMilliseconds,
                context.Input.Length);
        }
    }
}
