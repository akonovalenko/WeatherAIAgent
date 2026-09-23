using System.Collections.Concurrent;
using WeatherAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Limits requests per user using a fixed one-minute sliding window.
/// </summary>
public sealed class RateLimitMiddleware : AgentMiddlewareBase<RateLimitMiddleware>
{
    public override int Order => 70;

    private const int MaxRequests = 10;
    private const string AnonymousUserKey = "anonymous";
    private static readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _requests = new();
    private int _requestCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitMiddleware"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    public RateLimitMiddleware(ILogger<RateLimitMiddleware> logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Invokes the middleware to enforce rate limiting for the specified user context.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public override async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var now = DateTimeOffset.UtcNow;

        var userKey = string.IsNullOrWhiteSpace(context.UserId)
            ? AnonymousUserKey
            : context.UserId;

        var timestamps = this._requests.GetOrAdd(
            userKey,
            _ => new List<DateTimeOffset>());

        lock (timestamps)
        {
            timestamps.RemoveAll(timestamp => now - timestamp >= TimeWindow);

            if (timestamps.Count >= MaxRequests)
            {
                this.Logger.LogWarning($"Rate limit exceeded for user {userKey}. CorrelationId: {context.CorrelationId}");

                return "Request limit exceeded.\nMaximum 10 requests per minute.\nPlease wait and try again.";
            }

            timestamps.Add(now);
        }

        // Avoid scanning the whole dictionary on every request.
        if (Interlocked.Increment(ref this._requestCounter) % 100 == 0)
        {
            CleanupInactiveUsers(now);
        }

        return await next();
    }

    /// <summary>
    /// Cleans up inactive users from the request tracking dictionary to prevent memory growth.
    /// </summary>
    /// <param name="now">The current date and time.</param>
    private void CleanupInactiveUsers(DateTimeOffset now)
    {
        foreach (var pair in this._requests)
        {
            lock (pair.Value)
            {
                pair.Value.RemoveAll(timestamp => now - timestamp >= TimeWindow);

                if (pair.Value.Count == 0)
                {
                    this._requests.TryRemove(
                        new KeyValuePair<string, List<DateTimeOffset>>(
                            pair.Key,
                            pair.Value));
                }
            }
        }
    }
}

