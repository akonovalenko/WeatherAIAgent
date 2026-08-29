using Microsoft.Extensions.Logging;
using WeatherAIAgent.Models;
using WeatherAgent.Services;

namespace WeatherAgent.Middleware;

/// <summary>
/// Retries the complete agent operation when a transient dependency failure occurs.
/// </summary>
public sealed class RetryMiddleware : IAgentMiddleware
{
    private const int MaxAttempts = 3;
    private readonly ILogger<RetryMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    public RetryMiddleware(ILogger<RetryMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware, retrying the agent operation on transient errors.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            context.Items["RetryAttempt"] = attempt;

            try
            {
                return await next();
            }
            catch (Exception ex) when (IsTransientError(ex) && attempt < MaxAttempts)
            {
                lastException = ex;

                var delay = GetDelay(attempt);
                this._logger.LogWarning(
                    ex,
                    "Transient error. Retrying agent request in {DelayMs} ms. Attempt {Attempt}/{MaxAttempts}. CorrelationId: {CorrelationId}",
                    delay.TotalMilliseconds,
                    attempt,
                    MaxAttempts,
                    context.CorrelationId);

                await Task.Delay(delay);
            }
            catch (Exception ex) when (IsTransientError(ex))
            {
                lastException = ex;
                break;
            }
        }

        throw new InvalidOperationException(
            $"Agent request failed after {MaxAttempts} attempts.",
            lastException);
    }

    /// <summary>
    /// Calculates the delay before the next retry attempt using exponential backoff.
    /// </summary>
    /// <param name="attempt">The current retry attempt number.</param>
    /// <returns>The delay before the next retry attempt.</returns>
    private static TimeSpan GetDelay(int attempt) =>
        TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));

    /// <summary>
    /// Determines whether the specified exception is a transient error that can be retried.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>true if the exception is a transient error; otherwise, false.</returns>
    private static bool IsTransientError(Exception ex)
    {
        if (ex is WeatherServiceException { IsTransient: true } ||
            ex is HttpRequestException ||
            ex is TimeoutException ||
            ex is TaskCanceledException)
        {
            return true;
        }

        // OpenAI-compatible SDKs can surface HTTP failures as SDK-specific exceptions.
        // Inspect a Status/StatusCode property without coupling this middleware to a provider SDK.
        var status = GetStatusCode(ex);
        if (status is 408 or 429 or >= 500 and <= 599)
            return true;

        return ex.InnerException is not null && IsTransientError(ex.InnerException);
    }

    /// <summary>
    /// Gets the HTTP status code from the exception if it has a Status or StatusCode property.
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns>The HTTP status code, or null if not found.</returns>
    private static int? GetStatusCode(Exception ex)
    {
        var property = ex.GetType().GetProperty("Status") ?? ex.GetType().GetProperty("StatusCode");
        var value = property?.GetValue(ex);

        return value switch
        {
            int status => status,
            _ when value is not null && int.TryParse(value.ToString(), out var status) => status,
            _ => null
        };
    }
}
