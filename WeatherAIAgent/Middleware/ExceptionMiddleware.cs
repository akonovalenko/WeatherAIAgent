using Microsoft.Extensions.Logging;
using WeatherAIAgent.Helpers;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Converts unhandled pipeline exceptions into a user-friendly response after retry processing is exhausted.
/// </summary>
public sealed class ExceptionMiddleware : IAgentMiddleware
{
    private readonly ILogger<ExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use for logging exceptions.</param>
    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle exceptions in the agent pipeline. If an exception occurs, it logs the error and returns a user-friendly message.   
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        try
        {
            return await next();
        }
        catch (OperationCanceledException)
        {
            this._logger.LogWarning(
                "Agent request was cancelled. CorrelationId: {CorrelationId}",
                context.CorrelationId);
            return "The request was cancelled.";
        }
        catch (Exception ex)
        {
            context.Items["AgentError"] = ErrorHelper.GetShortError(ex);

            this._logger.LogError(
                ex,
                "Unhandled agent error. CorrelationId: {CorrelationId}",
                context.CorrelationId);

            return ErrorHelper.GetUserFriendlyMessage(ex);
        }
    }
}
