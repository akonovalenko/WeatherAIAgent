using Microsoft.Extensions.Logging;
using WeatherAgent.Models;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Reports token usage accumulated across all LLM calls in the current request.
/// </summary>
public sealed class TokenUsageMiddleware : IAgentMiddleware
{
    private readonly ILogger<TokenUsageMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenUsageMiddleware"/> class with the specified logger.       
    /// </summary>
    /// <param name="logger"></param>
    public TokenUsageMiddleware(ILogger<TokenUsageMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to report token usage after the next middleware in the pipeline has been executed.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var result = await next();

        if (context.Items.TryGetValue("TokenUsage", out var value) && value is TokenUsageInfo usage)
        {
            this._logger.LogInformation(
                "Token usage: {TotalTokens} tokens; estimated cost: ${EstimatedCost}",
                usage.TotalTokens,
                usage.EstimatedCost.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return result;
    }
}
