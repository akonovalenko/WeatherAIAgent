using WeatherAgent.Models;
using WeatherAIAgent.Interfaces;

namespace WeatherAgent.Middleware;

/// <summary>
/// Assigns a correlation ID. Lifecycle logging belongs to LoggingMiddleware.
/// </summary>
public sealed class CorrelationMiddleware : IAgentMiddleware
{
    public int Order => 10;

    /// <summary>
    /// Assigns a correlation ID to the context if it is not already set. This ensures that each request can be uniquely identified and traced through the system.  
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        if (string.IsNullOrWhiteSpace(context.CorrelationId))
        {
            context.CorrelationId = Guid.NewGuid().ToString("N");
        }

        return next();
    }
}

