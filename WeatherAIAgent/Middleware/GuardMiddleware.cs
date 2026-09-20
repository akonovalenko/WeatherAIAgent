using WeatherAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Performs cheap input validation before any sanitization or downstream work.
/// </summary>
public sealed class GuardMiddleware : AgentMiddlewareBase<GuardMiddleware>
{
    public override int Order => 50;

    private const int MinInputLength = 3;
    private const int MaxInputLength = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="GuardMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use for logging validation messages.</param>
    public GuardMiddleware(ILogger<GuardMiddleware> logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Invokes the middleware to validate the user input in the context. If the input is invalid, it logs a warning and returns an appropriate message. Otherwise, it calls the next middleware in the pipeline.   
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public override async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        if (string.IsNullOrWhiteSpace(context.Input))
        {
            this.Logger.LogWarning("Empty user input received. CorrelationId: {CorrelationId}", context.CorrelationId);
            return "Please enter a valid request.";
        }

        if (context.Input.Length > MaxInputLength)
        {
            this.Logger.LogWarning(
                "Input too long: {Length}. CorrelationId: {CorrelationId}",
                context.Input.Length,
                context.CorrelationId);
            return $"Input is too long. Maximum allowed length is {MaxInputLength} characters.";
        }

        if (context.Input.Trim().Length < MinInputLength)
        {
            this.Logger.LogWarning("Input too short. CorrelationId: {CorrelationId}", context.CorrelationId);
            return $"Input is too short. Minimum allowed length is {MinInputLength} characters.";
        }

        context.Input = context.Input.Trim();
        return await next();
    }
}
