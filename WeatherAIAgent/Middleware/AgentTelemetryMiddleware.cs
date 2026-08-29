using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Captures request duration and basic agent telemetry.
/// </summary>
public sealed class AgentTelemetryMiddleware : IAgentMiddleware
{
    private readonly ILogger<AgentTelemetryMiddleware> _logger;

    public AgentTelemetryMiddleware(ILogger<AgentTelemetryMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to capture request duration and log basic agent telemetry.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();
            stopwatch.Stop();
            context.Items["DurationMs"] = stopwatch.ElapsedMilliseconds;

            this._logger.LogInformation(
                "Agent metrics: CorrelationId={CorrelationId}, DurationMs={DurationMs}, InputLength={InputLength}",
                context.CorrelationId,
                stopwatch.ElapsedMilliseconds,
                context.Input.Length);

            return result;
        }
        catch
        {
            stopwatch.Stop();
            context.Items["DurationMs"] = stopwatch.ElapsedMilliseconds;
            throw;
        }
    }
}
