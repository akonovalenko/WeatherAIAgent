namespace WeatherAIAgent.Exceptions;

/// <summary>
/// Indicates that an AI agent invocation exceeded the application timeout.
/// This is intentionally not treated as a transient error by RetryMiddleware.
/// </summary>
public sealed class AgentTimeoutException : TimeoutException
{
    public AgentTimeoutException(string message) : base(message)
    {
    }
}
