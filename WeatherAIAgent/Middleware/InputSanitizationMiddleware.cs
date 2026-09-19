using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using WeatherAgent.Models;
using WeatherAIAgent.Interfaces;

namespace WeatherAgent.Middleware;

/// <summary>
/// Normalizes unsafe control characters and whitespace without pretending to protect against prompt injection.
/// </summary>
public sealed class InputSanitizationMiddleware : IAgentMiddleware
{
    public int Order => 60;

    private readonly ILogger<InputSanitizationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputSanitizationMiddleware"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for logging sanitization messages.</param>
    public InputSanitizationMiddleware(
        ILogger<InputSanitizationMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to sanitize the user input in the <see cref="AgentContext"/>.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result returned by the next middleware.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var originalInput = context.Input;
        var sanitizedInput = Sanitize(originalInput);

        var inputWasSanitized = !string.Equals(
            originalInput,
            sanitizedInput,
            StringComparison.Ordinal);

        context.Input = sanitizedInput;

        if (inputWasSanitized)
        {
            this._logger.LogInformation(
                "User input was normalized. CorrelationId: {CorrelationId}",
                context.CorrelationId);
        }

        return await next();
    }

    /// <summary>
    /// Sanitizes the input string by removing control characters
    /// except for newline, carriage return, and tab, normalizing
    /// whitespace, and trimming leading/trailing spaces.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>The sanitized string.</returns>
    private static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        input = new string(
            input
                .Where(c => !char.IsControl(c) || c is '\n' or '\r' or '\t')
                .ToArray());

        input = Regex.Replace(input, @"\s+", " ");

        return input.Trim();
    }
}
