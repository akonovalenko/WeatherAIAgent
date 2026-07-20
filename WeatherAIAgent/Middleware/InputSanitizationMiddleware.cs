using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Middleware responsible for sanitizing user input before
/// passing it to the agent.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
public sealed class InputSanitizationMiddleware : IAgentMiddleware
{
    private readonly ILogger<InputSanitizationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputSanitizationMiddleware"/> class with the specified logger.    
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    public InputSanitizationMiddleware(ILogger<InputSanitizationMiddleware> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to sanitize the user input in the given <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var originalInput = context.Input;
        var sanitizedInput = Sanitize(context.Input);

        if (!string.Equals(originalInput, sanitizedInput, StringComparison.Ordinal))
        {
            this._logger.LogInformation("User input was sanitized. CorrelationId: {CorrelationId}", context.CorrelationId);
            context.Items["InputWasSanitized"] = true;
        }
        else
        {
            context.Items["InputWasSanitized"] = false;
        }
        context.Input = sanitizedInput;
        return await next();
    }

    /// <summary>
    /// Sanitizes the input string by removing potentially harmful content, such as HTML tags, script tags, and excessive whitespace.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>The sanitized string.</returns>
    private static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove null and control characters
        input = new string(input.Where(c => !char.IsControl(c) || c == '\n').ToArray());

        // Remove HTML/script tags
        input = Regex.Replace(
            input,
            "<.*?>",
            string.Empty,
            RegexOptions.IgnoreCase);


        // Normalize whitespace
        input = Regex.Replace(
            input,
            @"\s+",
            " ");


        // Limit repeated symbols
        input = Regex.Replace(
            input,
            @"(.)\1{5,}",
            "$1$1$1");


        return input.Trim();
    }
}