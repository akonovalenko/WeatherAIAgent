namespace WeatherAIAgent.Helpers
{
    /// <summary>
    /// Provides helper methods for error handling and logging.
    /// </summary>
    public class ErrorHelper
    {
        /// <summary>
        /// Gets a short error message from an exception, including its type and message, and optionally a status code if available.    
        /// </summary>
        /// <param name="ex">The exception for which to get a short error message.</param>
        /// <returns>The short error message.</returns>
        public static string GetShortError(Exception ex)
        {
            var type = ex.GetType();
            var statusProp = type.GetProperty("Status") ?? type.GetProperty("StatusCode");
            var status = statusProp?.GetValue(ex);
            var baseMsg = $"{ex.Source} {type.Name}: {ex.Message}";
            return status != null ? $"{baseMsg} (Status: {status})" : baseMsg;
        }

        /// <summary>
        /// Combines error messages from OpenAI and weather service into a single string for logging or user display.
        /// </summary>
        /// <param name="llmError">The error message from LLM.</param>
        /// <param name="weatherError">The error message from the weather service.</param>
        /// <returns>The combined error message.</returns>
        public static string CombineErrors(string? llmError, string? weatherError)
        {
            if (!string.IsNullOrEmpty(llmError) && !string.IsNullOrEmpty(weatherError))
            {
                return $"Errors:\n- OpenAI: {llmError}\n- Weather service: {weatherError}\n";
            }

            if (!string.IsNullOrEmpty(llmError))
                return $"Error: {llmError}\n";

            if (!string.IsNullOrEmpty(weatherError))
                return $"Error: {weatherError}\n";

            return "Error: Unknown failure\n";
        }

    }
}
