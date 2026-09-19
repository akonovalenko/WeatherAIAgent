using Microsoft.Extensions.AI;
using WeatherAgent.Models;

namespace WeatherAIAgent.Interfaces;

/// <summary>
/// Interface for a weather tool that provides functionality to create AI functions related to weather information.
/// </summary>
public interface IWeatherTool
{
    /// <summary>
    /// Creates an AI function based on the provided agent context and cancellation token.
    /// </summary>
    /// <param name="context">The context of the agent.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An AI function.</returns>
    AIFunction Create(AgentContext context, CancellationToken cancellationToken);
}
