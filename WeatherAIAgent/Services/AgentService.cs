using OpenAI.Chat;
using System.Text.Json;
using WeatherAgent.Helpers;
using WeatherAgent.Models;
using WeatherAIAgent.Helpers;
using WeatherAIAgent.Models;

namespace WeatherAgent.Services;

/// <summary>
/// AI weather agent service with tool calling and token usage tracking.
/// Exceptions are intentionally allowed to propagate so RetryMiddleware can handle transient failures.
/// </summary>
public sealed class AgentService
{
    private const string WeatherToolName = "GetCurrentWeather";

    private readonly ChatClient _chatClient;
    private readonly IWeatherService _weatherService;

    private const string SystemPrompt = """
        You are a weather assistant.

        Always respond in user language.

        When the user asks about weather conditions,
        you must call the GetCurrentWeather function.

        Never guess weather information.

        Always use the function when weather data is required.

        Tool output formatting rules:
        - The result returned by GetCurrentWeather is already formatted.
        - Treat the tool result as final output.
        - Preserve the original formatting exactly.
        """;

    public AgentService(
        IChatClientFactory chatClientFactory,
        IWeatherService weatherService)
    {
        _chatClient = chatClientFactory.Create();
        _weatherService = weatherService;
    }

    public async Task<string> AskAsync(AgentContext context)
    {
        var tool = CreateWeatherTool();

        List<ChatMessage> messages =
        [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(context.Input)
        ];

        var completion = await _chatClient.CompleteChatAsync(
            messages,
            new ChatCompletionOptions { Tools = { tool } });

        SaveUsage(context, completion);

        if (completion.Value.FinishReason != ChatFinishReason.ToolCalls)
            return GetCompletionText(completion);

        // The assistant message containing ALL tool calls must be added exactly once.
        messages.Add(new AssistantChatMessage(completion));

        var toolCalls = completion.Value.ToolCalls;
        if (toolCalls.Count == 0)
            throw new InvalidOperationException("The model indicated tool calls but returned no tool calls.");

        foreach (var call in toolCalls)
        {
            if (!string.Equals(call.FunctionName, WeatherToolName, StringComparison.Ordinal))
                throw new InvalidOperationException($"Unsupported tool '{call.FunctionName}'.");

            context.Items["ToolName"] = call.FunctionName;
            context.Items["ToolCallId"] = call.Id;

            var location = ParseLocation(call.FunctionArguments);
            context.WeatherLocations.Add(location);
            context.Items["WeatherLocation"] = location;

            var weather = await _weatherService.GetCurrentWeatherAsync(location);
            var toolResult = FormatWeather(weather);

            messages.Add(new ToolChatMessage(call.Id, toolResult));
        }

        var finalAnswer = await _chatClient.CompleteChatAsync(messages);
        SaveUsage(context, finalAnswer);

        return GetCompletionText(finalAnswer);
    }

    private static ChatTool CreateWeatherTool() =>
        ChatTool.CreateFunctionTool(
            functionName: WeatherToolName,
            functionDescription: "Get current weather by city name",
            functionParameters: BinaryData.FromObjectAsJson(new
            {
                type = "object",
                properties = new
                {
                    location = new
                    {
                        type = "string",
                        description = "City name"
                    }
                },
                required = new[] { "location" }
            }));

    private static string ParseLocation(BinaryData arguments)
    {
        try
        {
            using var json = JsonDocument.Parse(arguments);

            if (!json.RootElement.TryGetProperty("location", out var locationElement) ||
                locationElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException("Weather tool arguments do not contain a valid 'location'.");
            }

            var location = locationElement.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(location))
                throw new InvalidOperationException("Weather tool returned an empty location.");

            return location;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Weather tool returned invalid JSON arguments.", ex);
        }
    }

    private static string GetCompletionText(ChatCompletion completion)
    {
        var text = completion.Content
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .Select(x => x.Text)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("The model returned an empty response.");

        return text;
    }

    private static string FormatWeather(WeatherInfo weather)
    {
        var airQuality = WeatherHelper.GetAirQualityDescription(weather.AirQualityIndex);

        return $"""
            Current weather information:

            Location:   {weather.Location}
            Region:     {weather.Region}
            Country:    {weather.Country}
            Latitude:   {weather.Latitude}
            Longitude:  {weather.Longitude}
            Timezone:   {weather.TimeZone}
            Local time: {weather.LocalTime}

            Temperature:
            - Current temperature: {weather.Temperature} °C
            - Feels like: {weather.FeelsLike} °C

            Weather condition:
            {weather.Condition}

            Atmospheric conditions:
            - Humidity: {weather.Humidity}%
            - Pressure: {weather.PressureMb} hPa
            - Cloud coverage: {weather.Cloud}%

            Wind conditions:
            - Wind speed: {weather.WindKph} km/h
            - Wind gust: {weather.GustKph} km/h
            - Wind direction: {weather.WindDirection}

            Visibility:
            - {weather.VisibilityKm} km

            Precipitation:
            - {weather.PrecipitationMm} mm

            UV information:
            - UV index: {weather.UvIndex}

            Air quality:
            - Status: {airQuality}
            - AQI: {weather.AirQualityIndex}
            - PM2.5: {weather.Pm25}
            - PM10: {weather.Pm10}
            """;
    }

    private static void SaveUsage(AgentContext context, ChatCompletion completion)
    {
        if (completion.Usage is null)
            return;

        var usageObj = completion.Usage;
        var inputTokens = AgentServiceHelpers.TryGetInt(
            usageObj, "InputTokenCount", "InputTokens", "PromptTokens", "PromptTokenCount");
        var outputTokens = AgentServiceHelpers.TryGetInt(
            usageObj, "OutputTokenCount", "OutputTokens", "CompletionTokens", "CompletionTokenCount");
        var totalTokens = AgentServiceHelpers.TryGetInt(
            usageObj, "TotalTokenCount", "TotalTokens", "total_tokens", "Total");

        if (totalTokens == 0)
            totalTokens = inputTokens + outputTokens;

        if (totalTokens <= 0)
            return;

        if (!context.Items.TryGetValue("TokenUsage", out var existingObj) || existingObj is not TokenUsageInfo usage)
        {
            usage = new TokenUsageInfo
            {
                Model = completion.Model ?? string.Empty
            };
            context.Items["TokenUsage"] = usage;
        }

        usage.InputTokens += inputTokens;
        usage.OutputTokens += outputTokens;
        usage.TotalTokens += totalTokens;

        if (string.IsNullOrWhiteSpace(usage.Model))
            usage.Model = completion.Model ?? string.Empty;

        usage.EstimatedCost = CalculateCost(usage.TotalTokens);

        context.Items["TotalTokensPerRequest"] = usage.TotalTokens;
        context.Items["EstimatedCostPerRequest"] = usage.EstimatedCost;
        context.Items["TotalTokens"] = usage.TotalTokens;
        context.Items["EstimatedCost"] = usage.EstimatedCost;
    }

    private static decimal CalculateCost(int totalTokens)
    {
        var env = Environment.GetEnvironmentVariable("PRICE_PER_1K_TOKENS");
        if (string.IsNullOrWhiteSpace(env) ||
            !decimal.TryParse(
                env,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var pricePer1K))
        {
            return 0m;
        }

        return Math.Round((totalTokens / 1000m) * pricePer1K, 6);
    }
}
