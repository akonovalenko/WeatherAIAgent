namespace WeatherAgent.Services;

public sealed class WeatherServiceException : Exception
{
    /// <summary>
    /// Optional numeric status (e.g. HTTP status code) for concise logging.
    /// </summary>
    public int? Status { get; }

    public WeatherServiceException(string message, int? status = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Status = status;
    }
}