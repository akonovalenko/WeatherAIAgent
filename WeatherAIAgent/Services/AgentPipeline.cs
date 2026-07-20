using WeatherAgent.Middleware;
using WeatherAIAgent.Models;

namespace WeatherAgent.Services;

public sealed class AgentPipeline
{
    private readonly IEnumerable<IAgentMiddleware> _middlewares;
    private readonly AgentService _agentService;

    /// <summary>
    /// Initializes a new instance of the AgentPipeline class with the specified middlewares and agent service.
    /// </summary>
    /// <param name="middlewares">The middlewares to include in the pipeline.</param>
    /// <param name="agentService">The agent service to use for processing requests.</param>
    public AgentPipeline(
        IEnumerable<IAgentMiddleware> middlewares,
        AgentService agentService)
    {
        this._middlewares = middlewares;
        this._agentService = agentService;
    }

    /// <summary>
    /// Executes the agent pipeline with the specified input, passing it through the configured middlewares and ultimately to the agent service for processing.
    /// </summary>
    /// <param name="input">The input for the agent request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task<string> ExecuteAsync(string input)
    {
        var context = new AgentContext
        {
            Input = input
        };

        var pipeline = 
            this._middlewares
           .Reverse()
           .Aggregate(
               () => _agentService.AskAsync(context),
               (next, middleware) =>
                   () => middleware.InvokeAsync(context, next));

        return pipeline();
    }
}