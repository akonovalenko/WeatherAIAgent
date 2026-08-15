using OpenAI.Chat;
using System.Text.Json;
using WeatherAgent.Helpers;
using WeatherAgent.Models;
using WeatherAIAgent.Helpers;
using WeatherAIAgent.Models;

namespace WeatherAgent.Services;

/// <summary>
/// AI weather agent service with tool calling and telemetry support.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
public sealed class AgentService
{
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

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentService"/> class with the specified chat client factory and weather service.
    /// </summary>
    /// <param name="chatClientFactory">The chat client factory.</param>
    /// <param name="weatherService">The weather service.</param>
    public AgentService(
        IChatClientFactory chatClientFactory,
        IWeatherService weatherService)
    {
        this._chatClient = chatClientFactory.Create();
        this._weatherService = weatherService;
    }

    /// <summary>
    /// Asks the AI agent a question and returns the response, handling tool calls and errors.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <returns>The response from the AI agent.</returns>
    public async Task<string> AskAsync(
        AgentContext context)
    {
        var tool =
            ChatTool.CreateFunctionTool(
                functionName: "GetCurrentWeather",
                functionDescription: "Get current weather by city name",
                functionParameters:
                    BinaryData.FromObjectAsJson(
                    new
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

        List<ChatMessage> messages =
        [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(context.Input)
        ];

        ChatCompletion completion;
        try
        {
            completion =
                await _chatClient.CompleteChatAsync(
                    messages,
                    new ChatCompletionOptions
                    {
                        Tools = { tool }
                    });
        }
        catch (Exception ex)
        {
            var shortError = ErrorHelper.GetShortError(ex);
            context.Items["AgentError"] = shortError;
            var weatherError = context.Items.TryGetValue("WeatherError", out var w) ? w?.ToString() : null;
            return ErrorHelper.CombineErrors(shortError, weatherError);
        }

        SaveUsage(context, completion);

        /*
         * Normal answer
         */
        if (completion.FinishReason !=
            ChatFinishReason.ToolCalls)
        {
            return completion.Content[0].Text;
        }

        /*
         * Tool execution
         */
        foreach (var call in completion.ToolCalls)
        {
            if (call.FunctionName !=
                "GetCurrentWeather")
            {
                continue;
            }
            context.Items["ToolName"] = call.FunctionName;
            context.Items["ToolCallId"] = call.Id;
            using JsonDocument json = JsonDocument.Parse(call.FunctionArguments);
            string location = json.RootElement.GetProperty("location").GetString()!;
            context.Items["WeatherLocation"] = location;
            WeatherInfo weather;
            try
            {
                weather = await _weatherService.GetCurrentWeatherAsync(location);
            }
            catch (Exception ex)
            {
                // Store concise weather service error and return it immediately.
                var weatherError = ErrorHelper.GetShortError(ex);
                context.Items["WeatherError"] = weatherError;
                return ErrorHelper.CombineErrors(null, weatherError);
            }
            context.WeatherLocation = location;

            string airQuality = WeatherHelper.GetAirQualityDescription(weather.AirQualityIndex);

            string toolResult =
             $"""
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

            messages.Add(new AssistantChatMessage(completion));
            messages.Add(new ToolChatMessage(call.Id, toolResult));
        }

        try
        {
            ChatCompletion finalAnswer = await this._chatClient.CompleteChatAsync(messages);
            SaveUsage(context, finalAnswer);
            return finalAnswer.Content[0].Text;
        }
        catch (Exception ex)
        {
            var openAiError = ErrorHelper.GetShortError(ex);
            context.Items["AgentError"] = openAiError;
            var weatherError = context.Items.TryGetValue("WeatherError", out var w) ? w?.ToString() : null;
            return ErrorHelper.CombineErrors(openAiError, weatherError);
        }
    }

    /// <summary>
    /// Saves the total token usage from the chat completion into the context items.        
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="completion">The chat completion.</param>
    private void SaveUsage(
    AgentContext context,
    ChatCompletion completion)
    {
        if (completion?.Usage == null)
            return;

        object usageObj = completion.Usage!;

        int inputTokens = AgentServiceHelpers.TryGetInt(usageObj, "InputTokenCount", "InputTokens", "PromptTokens", "PromptTokenCount");
        int outputTokens = AgentServiceHelpers.TryGetInt(usageObj, "OutputTokenCount", "OutputTokens", "CompletionTokens", "CompletionTokenCount");
        int totalTokens = AgentServiceHelpers.TryGetInt(usageObj, "TotalTokenCount", "TotalTokens", "total_tokens", "Total");

        if (totalTokens == 0)
            totalTokens = inputTokens + outputTokens;

        if (totalTokens <= 0)
            return;

        var usage = new TokenUsageInfo
        {
            Model = completion.Model ?? string.Empty,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = totalTokens,
            EstimatedCost = 0m
        };

        var env = Environment.GetEnvironmentVariable("PRICE_PER_1K_TOKENS");
        if (!string.IsNullOrWhiteSpace(env)
            && decimal.TryParse(env, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var pricePer1k))
        {
            usage.EstimatedCost = Math.Round((usage.TotalTokens / 1000m) * pricePer1k, 6);
        }

        // per-request usage and cost
        context.Items["TokenUsage"] = usage;
        context.Items["TotalTokensPerRequest"] = usage.TotalTokens;
        context.Items["EstimatedCostPerRequest"] = usage.EstimatedCost;

    }
}