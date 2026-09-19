using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using WeatherAgent.Models;
using WeatherAIAgent.Exceptions;
using WeatherAIAgent.Interfaces;

namespace WeatherAgent.Services;

public sealed class AgentService
{
    #region Private members

    private readonly IAIAgentFactory _agentFactory;
    private readonly IWeatherTool _weatherTool;
    private readonly ILogger<AgentService> _logger;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentService"/> class with the specified dependencies.
    /// </summary>
    /// <param name="agentFactory">The AI agent factory.</param>
    /// <param name="weatherTool">The weather tool.</param>
    /// <param name="logger">The logger.</param>
    public AgentService(
        IAIAgentFactory agentFactory,
        IWeatherTool weatherTool,
        ILogger<AgentService> logger)
    {
        this._agentFactory = agentFactory;
        this._weatherTool = weatherTool;
        this._logger = logger;
    }

    /// <summary>
    /// Asks the AI agent to process the input in the given context and returns the formatted weather information.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <returns>The formatted weather information.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="AgentTimeoutException"></exception>
    public async Task<string> AskAsync(AgentContext context)
    {
        var timeoutSeconds = context.Metadata.TimeoutSeconds;

        using var timeoutCts =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(timeoutSeconds));

        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                timeoutCts.Token,
                context.CancellationToken);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            this._logger.LogDebug(
                "Creating weather tool and AIAgent. CorrelationId={CorrelationId}",
                context.CorrelationId);

            var weatherTool =
                this._weatherTool.Create(
                    context,
                    linkedCts.Token);

            this._logger.LogDebug(
                "Weather tool created. CorrelationId={CorrelationId}",
                context.CorrelationId);

            var agent = this._agentFactory.Create([weatherTool]);

            this._logger.LogDebug(
                "AIAgent created. CorrelationId={CorrelationId}",
                context.CorrelationId);

            this._logger.LogInformation(
                "Starting AIAgent.RunAsync. CorrelationId={CorrelationId}, " +
                "ProviderTimeoutSeconds={TimeoutSeconds}, InputLength={InputLength}",
                context.CorrelationId,
                timeoutSeconds,
                context.Input.Length);

            var agentStopwatch =
                System.Diagnostics.Stopwatch.StartNew();

            try
            {
                this._logger.LogInformation(
                    "LLM/agent execution started. CorrelationId={CorrelationId}",
                    context.CorrelationId);

                var response = await agent.RunAsync(
                    context.Input,
                    cancellationToken: linkedCts.Token);

                agentStopwatch.Stop();

                this._logger.LogInformation(
                    "AIAgent.RunAsync returned. CorrelationId={CorrelationId}, " +
                    "AgentDurationMs={DurationMs}",
                    context.CorrelationId,
                    agentStopwatch.ElapsedMilliseconds);

                SaveUsage(context, response);
            }
            catch
            {
                agentStopwatch.Stop();

                this._logger.LogWarning(
                    "AIAgent.RunAsync failed. CorrelationId={CorrelationId}, " +
                    "AgentDurationMs={DurationMs}",
                    context.CorrelationId,
                    agentStopwatch.ElapsedMilliseconds);

                throw;
            }

            var weather = context.Weather;

            this._logger.LogInformation(
                "Agent execution state after RunAsync. " +
                "CorrelationId={CorrelationId}, ToolCalls={ToolCalls}, " +
                "HasFormattedWeather={HasFormattedWeather}",
                context.CorrelationId,
                weather.ToolCallCount,
                !string.IsNullOrWhiteSpace(weather.FormattedWeather));

            if (weather.ToolCallCount <= 0)
            {
                throw new InvalidOperationException(
                    "The agent did not invoke the required weather tool.");
            }

            if (string.IsNullOrWhiteSpace(weather.FormattedWeather))
            {
                throw new InvalidOperationException(
                    "The weather tool was invoked but did not produce an authoritative response.");
            }

            this._logger.LogInformation(
                "AIAgent.RunAsync completed. CorrelationId={CorrelationId}, " +
                "DurationMs={DurationMs}, ToolCalls={ToolCalls}",
                context.CorrelationId,
                stopwatch.ElapsedMilliseconds,
                weather.ToolCallCount);

            this._logger.LogInformation(
                "Returning authoritative weather result. " +
                "CorrelationId={CorrelationId}, TotalDurationMs={DurationMs}",
                context.CorrelationId,
                stopwatch.ElapsedMilliseconds);

            return weather.FormattedWeather;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            this._logger.LogWarning(
                "Agent application timeout reached. CorrelationId={CorrelationId}, " +
                "TimeoutSeconds={TimeoutSeconds}, DurationMs={DurationMs}",
                context.CorrelationId,
                timeoutSeconds,
                stopwatch.ElapsedMilliseconds);

            throw new AgentTimeoutException(
                $"The AI agent did not complete within {timeoutSeconds} seconds.");
        }
        catch (Exception ex)
        {
            context.Metadata.AgentError = ex.Message;

            this._logger.LogError(
                ex,
                "AgentService failed. CorrelationId={CorrelationId}, " +
                "DurationMs={DurationMs}, ToolCalls={ToolCalls}",
                context.CorrelationId,
                stopwatch.ElapsedMilliseconds,
                context.Weather.ToolCallCount);

            throw;
        }
        finally
        {
            stopwatch.Stop();
            context.Metadata.DurationMs =
                stopwatch.ElapsedMilliseconds;
        }
    }

    /// <summary>
    /// Saves the token usage information from the agent response into the context metadata.
    /// </summary>
    /// <param name="context">The agent context.</param>
    /// <param name="response">The agent response.</param>
    private static void SaveUsage(
        AgentContext context,
        AgentResponse response)
    {
        if (response.Usage is null)
        {
            return;
        }

        var usage = new TokenUsageInfo
        {
            InputTokens = (int)(response.Usage.InputTokenCount ?? 0L),
            OutputTokens = (int)(response.Usage.OutputTokenCount ?? 0L),
            TotalTokens = (int)(response.Usage.TotalTokenCount ?? 0L)
        };

        if (usage.TotalTokens == 0)
        {
            usage.TotalTokens =
                usage.InputTokens +
                usage.OutputTokens;
        }

        if (usage.TotalTokens <= 0)
        {
            return;
        }

        usage.EstimatedCost =
            CalculateCost(usage.TotalTokens);

        context.Metadata.TokenUsage = usage;
    }

    /// <summary>
    /// Calculates the estimated cost based on the total number of tokens used.
    /// </summary>
    /// <param name="totalTokens">The total number of tokens used.</param>
    /// <returns>The estimated cost.</returns>
    private static decimal CalculateCost(long totalTokens)
    {
        var env =
            Environment.GetEnvironmentVariable(
                "PRICE_PER_1K_TOKENS");

        if (string.IsNullOrWhiteSpace(env) ||
            !decimal.TryParse(
                env,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var pricePer1K))
        {
            return 0m;
        }

        return Math.Round(
            (totalTokens / 1000m) * pricePer1K,
            6);
    }
}
