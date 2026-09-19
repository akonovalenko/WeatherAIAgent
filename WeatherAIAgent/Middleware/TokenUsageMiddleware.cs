using Microsoft.Extensions.Logging;
using WeatherAgent.Models;
using WeatherAIAgent.Interfaces;

namespace WeatherAgent.Middleware;

/// <summary>
/// Reports token usage collected by AgentService.
/// </summary>
public sealed class TokenUsageMiddleware : IAgentMiddleware
{
    public int Order => 100;

    private readonly ILogger<TokenUsageMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenUsageMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    public TokenUsageMiddleware(ILogger<TokenUsageMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to log token usage information after the next middleware in the pipeline has been executed.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var result = await next();
        var usage = context.Metadata.TokenUsage;

        if (usage is not null)
        {
            this._logger.LogInformation(
                "\nToken usage: InputTokens={InputTokens}, OutputTokens={OutputTokens}, TotalTokens={TotalTokens}, EstimatedCost={EstimatedCost}\n",
                usage.InputTokens,
                usage.OutputTokens,
                usage.TotalTokens,
                usage.EstimatedCost.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return result;
    }
}
