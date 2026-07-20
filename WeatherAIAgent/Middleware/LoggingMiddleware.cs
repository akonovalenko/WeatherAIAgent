using Microsoft.Extensions.Logging;
using WeatherAIAgent.Models;

namespace WeatherAgent.Middleware;

/// <summary>
/// Represents a middleware component that logs the input and output of an agent request, as well as any exceptions that occur during processing.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public sealed class LoggingMiddleware : IAgentMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingMiddleware"/> class with the specified logger.  
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        this._logger = logger;
    }


    /// <summary>
    /// Processes the input and invokes the next middleware in the pipeline asynchronously, while logging the input, output, and any exceptions that occur during processing.
    /// </summary>
    /// <param name="context">The agent context containing the input and other information.</param>
    /// <param name="next">The next function in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<string> InvokeAsync(
        AgentContext context,
        Func<Task<string>> next)
    {
        var start = DateTime.UtcNow;
        this._logger.LogInformation("Agent request started: {Input}", context.Input);

        try
        {
            var result = await next();
            var elapsed = DateTime.UtcNow - start;

            this._logger.LogInformation(
                "Agent request completed in {ElapsedMs} ms",
                elapsed.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Agent request failed");
            throw;
        }
    }
}