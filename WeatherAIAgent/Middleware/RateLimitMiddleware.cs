using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Limits the number of agent requests per user.
/// Default limit: 10 requests per minute.
/// </summary>
public sealed class RateLimitMiddleware : IAgentMiddleware
{
    private readonly ILogger<RateLimitMiddleware> _logger;
    private const int MaxRequests = 10;
    private static readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, List<DateTime>> _requests = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    public RateLimitMiddleware(
        ILogger<RateLimitMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to check the rate limit for the user and either allows the request to proceed or returns an error message if the limit is exceeded.  
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var userId = context.UserId;
        var now = DateTime.UtcNow;
        var timestamps = this._requests.GetOrAdd(userId,_ => new List<DateTime>());
        lock (timestamps)
        {
            timestamps.RemoveAll(x => now - x > TimeWindow);

            if (timestamps.Count >= MaxRequests)
            {
                this._logger.LogWarning("Rate limit exceeded for user {UserId}", userId);

               return """
                Request limit exceeded.
                Maximum 10 requests per minute.
                Please wait and try again.
                """;
            }
            timestamps.Add(now);
        }
        return await next();
    }
}