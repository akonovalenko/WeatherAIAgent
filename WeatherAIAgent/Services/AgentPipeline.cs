using WeatherAgent.Middleware;
using WeatherAIAgent.Models;

namespace WeatherAgent.Services;

/// <summary>
/// Builds and executes the agent middleware pipeline.
/// </summary>
public sealed class AgentPipeline
{
    private readonly IEnumerable<IAgentMiddleware> _middlewares;
    private readonly AgentService _agentService;

    public AgentPipeline(
        IEnumerable<IAgentMiddleware> middlewares,
        AgentService agentService)
    {
        _middlewares = middlewares;
        _agentService = agentService;
    }

    public Task<string> ExecuteAsync(string input)
    {
        var context = new AgentContext { Input = input };

        var pipeline = _middlewares
            .Reverse()
            .Aggregate(
                () => _agentService.AskAsync(context),
                (next, middleware) => () => middleware.InvokeAsync(context, next));

        return pipeline();
    }
}
