using System.Text.RegularExpressions;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Validates that a weather response references every location requested through the weather tool.
/// </summary>
public sealed class OutputValidationMiddleware : IAgentMiddleware
{
    /// <summary>
    /// Validates that a weather response references every location requested through the weather tool.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <returns>The result of the middleware execution.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var output = await next();

        if (context.WeatherLocations.Count == 0)
            return output;

        foreach (var location in context.WeatherLocations)
        {
            if (!ContainsLocation(output, location))
            {
                return $"Output validation failed. The response does not match the expected location '{location}'.";
            }
        }

        return output;
    }

    /// <summary>
    /// Checks if the output contains the specified location, ignoring case and ensuring that the location is not part of a larger word.        
    /// </summary>
    /// <param name="output">The output string to search.</param>
    /// <param name="location">The location to search for.</param>
    /// <returns>true if the location is found; otherwise, false.</returns>
    private static bool ContainsLocation(string output, string location)
    {
        if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(location))
            return false;

        var pattern = $@"(?<!\w){Regex.Escape(location)}(?!\w)";
        return Regex.IsMatch(output, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
