using System.Text.RegularExpressions;
using WeatherAgent.Middleware;
using WeatherAIAgent.Models;

namespace WeatherAIAgent.Middleware;

/// <summary>
/// Middleware for validating the output of the weather agent.
/// </summary>
public sealed class OutputValidationMiddleware: IAgentMiddleware
{
    /// <summary>
    /// Validates the output of the weather agent to ensure it contains the expected location.  
    /// </summary>
    /// <param name="context">The context of the agent.</param>
    /// <param name="output">The output to validate.</param>
    /// <returns>The validated output.</returns>
    public async Task<string> InvokeAsync(
         AgentContext context,
         Func<Task<string>> next)
    {
        var output = await next();

        var expectedLocation = context.WeatherLocation;

        if (string.IsNullOrWhiteSpace(expectedLocation))
            return output;


        if (!IsCorrectLocation(
                output,
                expectedLocation))
        {
            return BuildValidationError(
                expectedLocation);
        }


        return output;
    }

    /// <summary>
    /// Checks if the output contains the expected location, ignoring case and ensuring it is a whole word match.
    /// </summary>
    /// <param name="output">The output to check.</param>
    /// <param name="expectedLocation">The expected location.</param>
    /// <returns>True if the location is correct, false otherwise.</returns>
    private bool IsCorrectLocation(string output, string expectedLocation)
    {
        if (string.IsNullOrWhiteSpace(output) ||
            string.IsNullOrWhiteSpace(expectedLocation))
        {
            return false;
        }

        var pattern =
            $@"(?<!\w){Regex.Escape(expectedLocation)}(?!\w)";

        return Regex.IsMatch(
            output,
            pattern,
            RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// This method builds a validation error message indicating that the output does not match the expected location.
    /// </summary>
    /// <param name="location">The expected location.</param>
    /// <returns></returns>
    private string BuildValidationError(string location)
    {
        return
            $"Output validation failed. " +
            $"The response does not match the expected location '{location}'. " +
            $"The weather forecast must only be generated for this location.";
    }
}