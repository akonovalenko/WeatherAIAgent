using Microsoft.Extensions.Logging;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Middleware that assigns a unique correlation identifier
/// to every agent request.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
public sealed class CorrelationMiddleware : IAgentMiddleware
{
    private readonly ILogger<CorrelationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    public CorrelationMiddleware(
        ILogger<CorrelationMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware, assigning a unique correlation identifier to the agent request and logging the start and completion of the request.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        context.CorrelationId = Guid.NewGuid().ToString();
        context.Items["StartedAt"] = DateTime.UtcNow;
        this._logger.LogInformation("Agent request started. CorrelationId: {CorrelationId}", context.CorrelationId);

        try
        {
            var result = await next();
            this._logger.LogInformation("Agent request completed. CorrelationId: {CorrelationId}", context.CorrelationId);
            return result;
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Agent request failed. CorrelationId: {CorrelationId}",
                context.CorrelationId);

            throw;
        }
    }
}