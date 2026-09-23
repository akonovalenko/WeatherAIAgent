using WeatherAgent.Models;
using WeatherAIAgent.Helpers;

namespace WeatherAgent.Middleware;

/// <summary>
/// Converts unhandled pipeline exceptions into a user-friendly response after retry processing is exhausted.
/// </summary>
public sealed class ExceptionMiddleware : AgentMiddlewareBase<ExceptionMiddleware>
{
    public override int Order => 30;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use for logging exceptions.</param>
    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Invokes the middleware to handle exceptions in the agent pipeline. If an exception occurs, it logs the error and returns a user-friendly message.   
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public override async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        try
        {
            return await next();
        }
        catch (OperationCanceledException)
        {
            this.Logger.LogWarning($"Agent request was cancelled. CorrelationId: {context.CorrelationId}");

            return "The request was cancelled.";
        }
        catch (Exception ex)
        {
            context.Metadata.AgentError = ErrorHelper.GetShortError(ex);

            this.Logger.LogError(ex, $"Unhandled agent error. CorrelationId: {context.CorrelationId}");

            return ErrorHelper.GetUserFriendlyMessage(ex);
        }
    }
}
