using WeatherAgent.Models;
using WeatherAIAgent.Interfaces;

namespace WeatherAgent.Middleware;

/// <summary>
/// Base class for middleware that requires a typed logger.
/// </summary>
/// <typeparam name="TMiddleware">The concrete middleware type used as the logging category.</typeparam>
public abstract class AgentMiddlewareBase<TMiddleware> : IAgentMiddleware
    where TMiddleware : class
{
    /// <summary>
    /// Gets the typed logger for the concrete middleware.
    /// </summary>
    protected ILogger<TMiddleware> Logger { get; }
    
    public abstract int Order { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentMiddlewareBase{TMiddleware}"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    protected AgentMiddlewareBase(ILogger<TMiddleware> logger)
    {
        this.Logger = logger;
    }

    /// <summary>
    /// Invokes the middleware logic asynchronously.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public abstract Task<string> InvokeAsync(AgentContext context, Func<Task<string>> next);

}
