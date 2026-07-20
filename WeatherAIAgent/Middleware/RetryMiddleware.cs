using Microsoft.Extensions.Logging;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Middleware that retries failed agent requests using exponential backoff.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
public sealed class RetryMiddleware : IAgentMiddleware
{
    private readonly ILogger<RetryMiddleware> _logger;

    private readonly int _maxRetries = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    public RetryMiddleware(ILogger<RetryMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle the agent request, retrying on transient errors.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next function in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        Exception? lastException = null;
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                context.Items["RetryAttempt"] = attempt;
                if (attempt > 1)
                {
                    this._logger.LogWarning(
                        "Retry attempt {Attempt}/{MaxRetries}. CorrelationId: {CorrelationId}",
                        attempt,
                        _maxRetries,
                        context.CorrelationId);
                }
                return await next();
            }
            catch (Exception ex) when (IsTransientError(ex))
            {
                lastException = ex;

                this._logger.LogWarning(
                    ex,
                    "Transient error during agent execution. Attempt {Attempt}/{MaxRetries}",
                    attempt,
                    _maxRetries);

                if (attempt == _maxRetries)
                    break;

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay);
            }
        }

        throw new InvalidOperationException(
            $"Agent request failed after {_maxRetries} retries.",
            lastException);
    }

    /// <summary>
    /// Determines whether the specified exception is a transient error that can be retried.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>true if the exception is a transient error; otherwise, false.</returns>
    private static bool IsTransientError(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => true,

            TaskCanceledException => true,

            TimeoutException => true,

            _ => false
        };
    }
}