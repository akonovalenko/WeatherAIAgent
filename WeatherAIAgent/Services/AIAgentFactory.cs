using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using WeatherAIAgent.Interfaces;
using WeatherAIAgent.Models;

namespace WeatherAgent.Services;

/// <summary>
/// Creates the application's AIAgent instances.
/// The agent uses the OpenAI Chat Completions API for broad
/// OpenAI-compatible provider compatibility.
/// </summary>
public sealed class AIAgentFactory : IAIAgentFactory
{
    private readonly ILLMProviderOptions _options;
    private readonly LLMOptions _llmOptions;

    // Оптимізований короткий промпт без суперечливих інструкцій та заборон.
    // Забезпечує миттєвий виклик тулу без довгого фази міркування (Reasoning).
    private const string SystemPrompt = """
        You are a weather assistant.
        Your only job is to call the GetCurrentWeather tool when the user asks for weather information.
        Do not generate any text response before calling the tool.
        """;

    public AIAgentFactory(
        IOptions<LLMOptions> llmOptions,
        IOptions<OpenAIOptions> openAiOptions,
        IOptions<NvidiaOptions> nvidiaOptions)
    {
        ArgumentNullException.ThrowIfNull(llmOptions);
        ArgumentNullException.ThrowIfNull(openAiOptions);
        ArgumentNullException.ThrowIfNull(nvidiaOptions);

        this._llmOptions = llmOptions.Value;
        this._options = llmOptions.Value.Provider.ToLowerInvariant() switch
        {
            "openai" => openAiOptions.Value,
            "nvidia" => nvidiaOptions.Value,
            _ => throw new InvalidOperationException(
                $"Unknown provider '{llmOptions.Value.Provider}'.")
        };
    }

    /// <summary>
    /// Adds diagnostics around every IChatClient call.
    /// SDK retries are disabled, so each call represents one provider attempt.
    /// </summary>
    private sealed class DiagnosticChatClient(IChatClient innerClient)
        : DelegatingChatClient(innerClient)
    {
        private long _requestNumber;

        public override async Task<ChatResponse> GetResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var requestNumber = Interlocked.Increment(ref _requestNumber);
            var stopwatch = Stopwatch.StartNew();
            var messageList = messages as IList<Microsoft.Extensions.AI.ChatMessage>
                ?? messages.ToList();

            Console.WriteLine(
                $"[AgentDiagnostics] LLM request started: " +
                $"Request={requestNumber}, MessageCount={messageList.Count}");

            try
            {
                var response = await base.GetResponseAsync(
                    messageList,
                    options,
                    cancellationToken);

                stopwatch.Stop();

                Console.WriteLine(
                    $"[AgentDiagnostics] LLM request completed: " +
                    $"Request={requestNumber}, " +
                    $"DurationMs={stopwatch.ElapsedMilliseconds}");

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                Console.WriteLine(
                    $"[AgentDiagnostics] LLM request failed: " +
                    $"Request={requestNumber}, " +
                    $"DurationMs={stopwatch.ElapsedMilliseconds}, " +
                    $"Error={ex.GetType().Name}: {ex.Message}");

                throw;
            }
        }
        
        public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var requestNumber = Interlocked.Increment(ref _requestNumber);
            var stopwatch = Stopwatch.StartNew();
            var messageList = messages as IList<Microsoft.Extensions.AI.ChatMessage> ?? messages.ToList();

            Console.WriteLine(
                $"[AgentDiagnostics] LLM streaming request started: " +
                $"Request={requestNumber}, MessageCount={messageList.Count}");

            var firstUpdate = true;
            long firstUpdateMs = 0;
            var updateCount = 0;

            try
            {
                await foreach (var update in base.GetStreamingResponseAsync(
                    messageList,
                    options,
                    cancellationToken))
                {
                    updateCount++;

                    if (firstUpdate)
                    {
                        firstUpdate = false;
                        firstUpdateMs = stopwatch.ElapsedMilliseconds;

                        Console.WriteLine(
                            $"[AgentDiagnostics] LLM first update received: " +
                            $"Request={requestNumber}, " +
                            $"TimeToFirstUpdateMs={firstUpdateMs}");
                    }

                    yield return update;
                }

                stopwatch.Stop();

                Console.WriteLine(
                    $"[AgentDiagnostics] LLM streaming request completed: " +
                    $"Request={requestNumber}, " +
                    $"TimeToFirstUpdateMs={firstUpdateMs}, " +
                    $"DurationMs={stopwatch.ElapsedMilliseconds}, " +
                    $"UpdateCount={updateCount}");
            }
            finally
            {
                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();

                    Console.WriteLine(
                        $"[AgentDiagnostics] LLM streaming request finished: " +
                        $"Request={requestNumber}, " +
                        $"TimeToFirstUpdateMs={firstUpdateMs}, " +
                        $"DurationMs={stopwatch.ElapsedMilliseconds}, " +
                        $"UpdateCount={updateCount}");
                }
            }
        }
    }
    
    /// <summary>
    /// Creates an AIAgent backed by an OpenAI-compatible Chat Completions client.
    /// </summary>
    public AIAgent Create(IList<AITool>? tools = null)
    {
        var apiKey = string.IsNullOrWhiteSpace(_options.ApiKey)
            ? Environment.GetEnvironmentVariable("LLM_API_KEY")
            : _options.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("API key is not configured.");

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException("Endpoint is not configured.");

        if (string.IsNullOrWhiteSpace(_options.Model))
            throw new InvalidOperationException("Model is not configured.");

        var endpoint = new Uri(_options.Endpoint, UriKind.Absolute);

        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = endpoint,
            NetworkTimeout = TimeSpan.FromSeconds(Math.Max(1, _llmOptions.AgentTimeoutSeconds))
        };

        Console.WriteLine(
            $"[AgentDiagnostics] Creating Chat Completions client. " +
            $"Model={_options.Model}, " +
            $"Endpoint={endpoint}, " +
            $"NetworkTimeoutSeconds={clientOptions.NetworkTimeout.Value.TotalSeconds}");

        var client = new ChatClient(
            _options.Model,
            new ApiKeyCredential(apiKey),
            clientOptions);

        IChatClient chatClient = new DiagnosticChatClient(client.AsIChatClient());

        Console.WriteLine("[AgentDiagnostics] ChatClient adapted to IChatClient. LLM call timing enabled.");

        AIAgent agent = new ChatClientAgent(
            chatClient,
            instructions: SystemPrompt,
            name: "WeatherAgent",
            description: "An AI agent that retrieves current weather information by city.",
            tools: tools);

        Console.WriteLine(
            $"[AgentDiagnostics] AIAgent created. " +
            $"ToolCount={tools?.Count ?? 0}");

        return agent.AsBuilder()
            .Use(static async (
                AIAgent agent,
                FunctionInvocationContext context,
                Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
                CancellationToken cancellationToken) =>
            {
                var stopwatch = Stopwatch.StartNew();

                Console.WriteLine(
                    $"[AgentDiagnostics] Tool invocation started: " +
                    $"{context.Function.Name}");

                try
                {
                    var result = await next(context, cancellationToken);

                    stopwatch.Stop();

                    Console.WriteLine(
                        $"[AgentDiagnostics] Tool invocation completed: " +
                        $"{context.Function.Name}, " +
                        $"DurationMs={stopwatch.ElapsedMilliseconds}, " +
                        $"HasResult={result is not null}");

                    if (string.Equals(
                            context.Function.Name,
                            "GetCurrentWeather",
                            StringComparison.Ordinal) &&
                        result is not null)
                    {
                        context.Terminate = true;

                        Console.WriteLine(
                            "[AgentDiagnostics] Agent termination requested " +
                            "after successful weather tool invocation.");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    Console.WriteLine(
                        $"[AgentDiagnostics] Tool invocation failed: " +
                        $"{context.Function.Name}, " +
                        $"DurationMs={stopwatch.ElapsedMilliseconds}, " +
                        $"Error={ex.GetType().Name}: {ex.Message}");

                    throw;
                }
            })
            .Build();
    }
}