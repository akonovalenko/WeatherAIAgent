using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Middleware that captures telemetry data for agent requests, including duration, input length, token usage, and estimated cost.
/// </summary>
public sealed class AgentTelemetryMiddleware 
    : IAgentMiddleware
{
    private readonly ILogger<AgentTelemetryMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentTelemetryMiddleware"/> class with the specified logger.   
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    public AgentTelemetryMiddleware(
        ILogger<AgentTelemetryMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware, capturing telemetry data for the agent request, including duration, input length, token usage, and estimated cost.
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
            var result = await next();
            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;
            context.Items["DurationMs"] = duration;

            // Metrics
            this._logger.LogInformation(
                """
                Agent Metrics:
                CorrelationId: {CorrelationId}
                Duration:      {Duration} ms
                Input length:  {InputLength}

                """,
                context.CorrelationId,
                duration,
                context.Input.Length);

            // Token usage
            if (context.Items.TryGetValue("TotalTokens", out var tokens))
            {
                this._logger.LogInformation("Token usage: {Tokens}", tokens);
            }

            // Cost control
            if (context.Items.TryGetValue("EstimatedCost", out var cost))
            {
                this._logger.LogInformation("Estimated cost: ${Cost}", cost);
            }

            return result;
        }
        catch(Exception ex)
        {
            this._logger.LogError(ex, "Agent telemetry captured failure");
            throw;
        }
    }
}