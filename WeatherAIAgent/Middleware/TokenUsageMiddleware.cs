using System;
using Microsoft.Extensions.Logging;
using WeatherAIAgent.Models;
using WeatherAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Reads the TokenUsage object saved by the agent service, computes estimated cost
/// only when missing, logs and prints a concise summary. It no longer overwrites
/// numeric keys when SaveUsage already set them.
/// </summary>
public sealed class TokenUsageMiddleware : IAgentMiddleware
{
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
    /// Invokes the middleware to process the agent context and compute token usage and estimated cost.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public async Task<string> InvokeAsync(AgentContext context, Func<Task<string>> next)
    {
        // Execute downstream first so TokenUsage is populated by SaveUsage
        var result = await next();

        int totalTokens = 0;
        decimal estimatedCost = 0m;

        if (context.Items.TryGetValue("TokenUsage", out var usageObj) && usageObj is TokenUsageInfo usage)
        {
            totalTokens = usage.TotalTokens;
            estimatedCost = usage.EstimatedCost;

            // Only compute estimated cost if it's not already computed by SaveUsage
            if (estimatedCost == 0m)
            {
                var env = Environment.GetEnvironmentVariable("PRICE_PER_1K_TOKENS");
                if (!string.IsNullOrWhiteSpace(env) &&
                    decimal.TryParse(env, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var pricePer1k))
                {
                    estimatedCost = Math.Round((usage.TotalTokens / 1000m) * pricePer1k, 6);
                    usage.EstimatedCost = estimatedCost;
                }
            }

            // Do not overwrite numeric keys if SaveUsage already set them.
            if (!context.Items.ContainsKey("TotalTokens"))
                context.Items["TotalTokens"] = totalTokens;
            if (!context.Items.ContainsKey("EstimatedCost"))
                context.Items["EstimatedCost"] = estimatedCost;
        }
        else
        {
            // Fallback: try numeric keys if TokenUsage is not present
            if (context.Items.TryGetValue("TotalTokens", out var t) && int.TryParse(t?.ToString() ?? "0", out var parsed))
                totalTokens = parsed;
            if (context.Items.TryGetValue("EstimatedCost", out var c) && decimal.TryParse(c?.ToString() ?? "0", System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsedC))
                estimatedCost = parsedC;
        }

        Console.WriteLine($"Token usage: {totalTokens} tokens \n" +
            $"Estimated cost: ${estimatedCost.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        return result;
    }
}