namespace WeatherAgent.Helpers;

public static class WeatherHelper
{
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