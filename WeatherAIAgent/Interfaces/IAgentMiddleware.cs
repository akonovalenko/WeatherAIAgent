using WeatherAgent.Models;

namespace WeatherAIAgent.Interfaces;

/// <summary>
/// Defines the interface for agent middleware that processes input and invokes the next middleware in the pipeline.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public interface IAgentMiddleware
{
    /// <summary>
    /// Gets the execution order. Lower values execute earlier on the way in.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Processes the input and invokes the next middleware in the pipeline.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next function in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<string> InvokeAsync(AgentContext context, Func<Task<string>> next);
}