namespace WeatherAIAgent.Exceptions;

/// <summary>
/// Represents an error returned or caused by the weather service.
/// </summary>
public sealed class WeatherServiceException : Exception
{
    public int? Status { get; }
    public bool IsTransient { get; }

    public WeatherServiceException(
        string message,
        int? status = null,
        bool isTransient = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Status = status;
        IsTransient = isTransient;
    }
}
