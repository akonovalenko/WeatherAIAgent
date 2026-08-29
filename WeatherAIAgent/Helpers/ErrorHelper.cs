using WeatherAgent.Services;

namespace WeatherAIAgent.Helpers;

/// <summary>
/// Provides helper methods for extracting error information from exceptions, 
/// including short error messages and user-friendly messages. 
/// It unwraps exceptions to find the root cause and checks for specific exception types to extract 
/// status codes and generate appropriate messages.
/// </summary>
public static class ErrorHelper
{
    /// <summary>
    /// Gets a short error message from the exception, including the exception type, message, and status code if available. It unwraps the exception to find the root cause and checks for specific exception types to extract the status code.
    /// </summary>
    /// <param name="ex">The exception to process.</param>
    /// <returns>The short error message.</returns>
    public static string GetShortError(Exception ex)
    {
        var status = FindStatus(ex);
        var message = ex.Message;
        var baseMessage = $"{ex.GetType().Name}: {message}";
        return status.HasValue ? $"{baseMessage} (Status: {status.Value})" : baseMessage;
    }

    /// <summary>
    /// Gets a user-friendly error message based on the type of exception. It unwraps the exception to find the root cause and returns a message suitable for display to the user.  
    /// </summary>
    /// <param name="ex">The exception to process.</param>
    /// <returns>The user-friendly error message.</returns>
    public static string GetUserFriendlyMessage(Exception ex)
    {
        var root = Unwrap(ex);

        return root switch
        {
            WeatherServiceException => "Unable to retrieve weather data. Please try again later.",
            HttpRequestException => "The external service is temporarily unavailable. Please try again later.",
            TimeoutException => "The request timed out. Please try again.",
            TaskCanceledException => "The request timed out. Please try again.",
            _ => "The agent could not complete the request. Please try again."
        };
    }

    /// <summary>
    /// Unwraps the exception to find the root cause, handling specific cases for InvalidOperationException and AggregateException. 
    /// </summary>
    /// <param name="ex">The exception to unwrap.</param>
    /// <returns>The root cause exception.</returns>
    private static Exception Unwrap(Exception ex)
    {
        while (ex is InvalidOperationException { InnerException: not null } or AggregateException)
        {
            ex = ex switch
            {
                AggregateException aggregate => aggregate.Flatten().InnerExceptions.FirstOrDefault() ?? aggregate,
                InvalidOperationException invalid when invalid.InnerException is not null => invalid.InnerException,
                _ => ex
            };

            if (ex.InnerException is null && ex is not AggregateException)
                break;
        }

        return ex;
    }

    /// <summary>
    /// Finds the status code from the exception or its inner exceptions, checking for WeatherServiceException and other exceptions with a Status or StatusCode property.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>The status code, or null if not found.</returns>
    private static int? FindStatus(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is WeatherServiceException weather && weather.Status.HasValue)
                return weather.Status;

            var property = current.GetType().GetProperty("Status") ?? current.GetType().GetProperty("StatusCode");
            if (property?.GetValue(current) is int intStatus)
                return intStatus;
        }

        return null;
    }
}
