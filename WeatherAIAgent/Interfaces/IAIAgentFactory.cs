using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace WeatherAIAgent.Interfaces;

/// <summary>
/// Creates the application AI agent.
/// </summary>
public interface IAIAgentFactory
{
    /// <summary>
    /// Creates the application AI agent with the specified tools.
    /// </summary>
    /// <param name="tools">The list of tools to include in the agent.</param>
    /// <returns>The created AI agent.</returns>
    AIAgent Create(IList<AITool>? tools = null);
}
