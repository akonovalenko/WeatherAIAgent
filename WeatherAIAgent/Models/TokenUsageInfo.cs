namespace WeatherAgent.Models;

/// <summary>
/// Represents information about token usage in a language model.
/// </summary>
public sealed class TokenUsageInfo
{
    public string Model { get; set; } = string.Empty;

    public int InputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int TotalTokens { get; set; }

    public decimal EstimatedCost { get; set; }
}