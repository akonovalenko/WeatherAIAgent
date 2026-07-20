using Microsoft.Extensions.Logging;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Represents a middleware component that guards against invalid or malicious input 
/// by validating the input string and returning an appropriate response if the input is invalid.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public sealed class GuardMiddleware : IAgentMiddleware
{
    private readonly ILogger<GuardMiddleware> _logger;
    private const int MinInputLength = 3;
    private const int MaxInputLength = 500;


    /// <summary>
    /// Initializes a new instance of the <see cref="GuardMiddleware"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    public GuardMiddleware(
        ILogger<GuardMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Processes the input and invokes the next middleware in the pipeline asynchronously, while validating the input string and returning an appropriate response if the input is invalid.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next function in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        if (string.IsNullOrWhiteSpace(context.Input))
        {
            this. _logger.LogWarning("Empty user input received");
            return "Please enter a valid request.";
        }

        if (context.Input.Length > MaxInputLength)
        {
            this._logger.LogWarning("Input too long: {Length}", context.Input.Length);
            return $"Input is too long. Maximum allowed length is {MaxInputLength} characters.";
        }

        if (context.Input.Length < MinInputLength)
        {
            this._logger.LogWarning("Input too short: {Length}", context.Input.Length);
            return $"Input is too short. Minimum allowed length is {MinInputLength} characters.";
        }

        var normalizedInput = context.Input.Trim();
        return await next();
    }
}