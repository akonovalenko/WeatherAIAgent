using WeatherAgent.Models;
using WeatherAIAgent.Interfaces;

namespace WeatherAgent.Services;

/// <summary>
/// Executes the agent through the configured middleware pipeline.
/// </summary>
public sealed class AgentPipeline
{
    private readonly IReadOnlyList<IAgentMiddleware> _middlewares;
    private readonly AgentService _agentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentPipeline"/> class with the specified middlewares and agent service.
    /// </summary>
    /// <param name="middlewares">The middlewares.</param>
    /// <param name="agentService">The agent service.</param>
    public AgentPipeline(
        IEnumerable<IAgentMiddleware> middlewares,
        AgentService agentService)
    {
        this._middlewares = middlewares
            .OrderBy(middleware => middleware.Order)
            .ToArray();

        this._agentService = agentService;

        Console.WriteLine("[AgentDiagnostics] Middleware pipeline: " +
            string.Join(" -> ", this._middlewares.Select(middleware => middleware.GetType().Name)));
    }

    /// <summary>
    /// Executes the agent pipeline with the specified input and cancellation token.
    /// </summary>
    /// <param name="input">The input for the agent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the agent execution.</returns>
    public Task<string> ExecuteAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        var context = new AgentContext
        {
            Input = input,
            CancellationToken = cancellationToken
        };

        Func<Task<string>> pipeline = () => this._agentService.AskAsync(context);

        foreach (var middleware in this._middlewares.Reverse())
        {
            var next = pipeline;
            pipeline = () => middleware.InvokeAsync(context, next);
        }

        return pipeline();
    }
}
