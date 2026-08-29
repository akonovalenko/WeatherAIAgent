namespace WeatherAgent.Helpers;

/// <summary>
/// Provides helper methods for weather-related operations.
/// </summary>
public static class WeatherHelper
{
    /// <summary>
    /// Gets a description of the air quality based on the provided AQI (Air Quality Index) value. 
    /// The method returns a string that describes the air quality level corresponding to the AQI value, 
    /// ranging from "Good" to "Hazardous". If the AQI value is outside the expected range, it returns "Unknown".
    /// </summary>
    /// <param name="aqi">The AQI value.</param>
    /// <returns>The air quality description.</returns>
    public static string GetAirQualityDescription(int aqi)
    {
        return aqi switch
        {
            1 => "Good",
            2 => "Moderate",
            3 => "Unhealthy for sensitive groups",
            4 => "Unhealthy",
            5 => "Very unhealthy",
            6 => "Hazardous",
            _ => "Unknown"
        };
    }
}