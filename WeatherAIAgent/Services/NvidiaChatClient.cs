using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WeatherAgent.Services;

public sealed class NvidiaChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _endpoint;
    private readonly string _apiKey;

    public NvidiaChatClient(
        HttpClient httpClient,
        string endpoint,
        string model,
        string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint.TrimEnd('/');
        _model = model;
        _apiKey = apiKey;

        if (string.IsNullOrWhiteSpace(_endpoint))
            throw new ArgumentException("Endpoint is required.", nameof(endpoint));

        if (string.IsNullOrWhiteSpace(_model))
            throw new ArgumentException("Model is required.", nameof(model));

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new ArgumentException("API key is required.", nameof(apiKey));
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestNumber = Guid.NewGuid().ToString("N")[..8];

        var request = CreateRequest(messages, options, stream: false);

        Console.WriteLine(
            $"[NvidiaDiagnostics] HTTP request started: " +
            $"Request={requestNumber}, Model={_model}");

        try
        {
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_endpoint}/chat/completions");

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var sendStopwatch = Stopwatch.StartNew();

            using var response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            sendStopwatch.Stop();

            Console.WriteLine(
                $"[NvidiaDiagnostics] HTTP response headers received: " +
                $"Request={requestNumber}, " +
                $"Status={(int)response.StatusCode}, " +
                $"HeadersMs={sendStopwatch.ElapsedMilliseconds}");

            var responseText = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"NVIDIA API returned {(int)response.StatusCode} " +
                    $"{response.ReasonPhrase}: {responseText}");
            }

            var result = JsonSerializer.Deserialize<NvidiaResponse>(
                responseText,
                JsonOptions);

            if (result is null)
                throw new InvalidOperationException(
                    "NVIDIA API returned an empty response.");

            var chatResponse = ConvertResponse(result);

            stopwatch.Stop();

            Console.WriteLine(
                $"[NvidiaDiagnostics] HTTP request completed: " +
                $"Request={requestNumber}, " +
                $"DurationMs={stopwatch.ElapsedMilliseconds}, " +
                $"PromptTokens={result.Usage?.PromptTokens ?? 0}, " +
                $"CompletionTokens={result.Usage?.CompletionTokens ?? 0}");

            return chatResponse;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Console.WriteLine(
                $"[NvidiaDiagnostics] HTTP request failed: " +
                $"Request={requestNumber}, " +
                $"DurationMs={stopwatch.ElapsedMilliseconds}, " +
                $"Error={ex.GetType().Name}: {ex.Message}");

            throw;
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestNumber = Guid.NewGuid().ToString("N")[..8];
        var firstUpdate = true;
        var updateCount = 0;

        var request = CreateRequest(messages, options, stream: true);

        Console.WriteLine(
            $"[NvidiaDiagnostics] HTTP streaming request started: " +
            $"Request={requestNumber}, Model={_model}");

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_endpoint}/chat/completions");

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        Console.WriteLine(
            $"[NvidiaDiagnostics] HTTP streaming headers received: " +
            $"Request={requestNumber}, " +
            $"Status={(int)response.StatusCode}, " +
            $"HeadersMs={stopwatch.ElapsedMilliseconds}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new HttpRequestException(
                $"NVIDIA API returned {(int)response.StatusCode} " +
                $"{response.ReasonPhrase}: {error}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);

        using var reader = new StreamReader(stream);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
                break;

            if (!line.StartsWith("data:", StringComparison.Ordinal))
                continue;

            var data = line[5..].Trim();

            if (data == "[DONE]")
                break;

            if (string.IsNullOrWhiteSpace(data))
                continue;

            NvidiaResponse? chunk;

            try
            {
                chunk = JsonSerializer.Deserialize<NvidiaResponse>(
                    data,
                    JsonOptions);
            }
            catch
            {
                continue;
            }

            if (chunk is null)
                continue;

            foreach (var update in ConvertStreamingResponse(chunk))
            {
                updateCount++;

                if (firstUpdate)
                {
                    firstUpdate = false;

                    Console.WriteLine(
                        $"[NvidiaDiagnostics] First update received: " +
                        $"Request={requestNumber}, " +
                        $"TimeToFirstUpdateMs={stopwatch.ElapsedMilliseconds}");
                }

                yield return update;
            }
        }

        stopwatch.Stop();

        Console.WriteLine(
            $"[NvidiaDiagnostics] HTTP streaming request completed: " +
            $"Request={requestNumber}, " +
            $"DurationMs={stopwatch.ElapsedMilliseconds}, " +
            $"UpdateCount={updateCount}");
    }

    public object? GetService(
        Type serviceType,
        object? serviceKey = null)
    {
        if (serviceType == typeof(ChatClientMetadata))
        {
            return new ChatClientMetadata(
                providerName: "NVIDIA",
                defaultModelId: _model);
        }

        return null;
    }

    public void Dispose()
    {
        // HttpClient lifetime is owned by the caller.
    }

    private NvidiaRequest CreateRequest(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        bool stream)
    {
        var request = new NvidiaRequest
        {
            Model = options?.ModelId ?? _model,
            Messages = messages.Select(ConvertMessage).ToList(),
            Stream = stream
        };

        if (options?.Temperature is { } temperature)
            request.Temperature = temperature;

        if (options?.TopP is { } topP)
            request.TopP = topP;

        if (options?.MaxOutputTokens is { } maxTokens)
            request.MaxTokens = maxTokens;

        if (options?.Tools is { Count: > 0 })
        {
            request.Tools = options.Tools
                .OfType<AIFunctionDeclaration>()
                .Select(CreateTool)
                .ToList();

            request.ToolChoice = "auto";
        }

        return request;
    }

    private static NvidiaTool CreateTool(AIFunctionDeclaration function)
    {
        return new NvidiaTool
        {
            Type = "function",
            Function = new NvidiaFunction
            {
                Name = function.Name,
                Description = function.Description,
                Parameters = function.JsonSchema
            }
        };
    }

    private static NvidiaMessage ConvertMessage(ChatMessage message)
    {
        var role = message.Role.Value;

        var result = new NvidiaMessage
        {
            Role = role switch
            {
                "system" => "system",
                "user" => "user",
                "assistant" => "assistant",
                "tool" => "tool",
                _ => "user"
            }
        };

        var text = new StringBuilder();

        foreach (var content in message.Contents)
        {
            switch (content)
            {
                case TextContent textContent:
                    text.Append(textContent.Text);
                    break;

                case FunctionResultContent resultContent:
                    result.Role = "tool";
                    result.ToolCallId = resultContent.CallId;
                    result.Content = resultContent.Result?.ToString() ?? string.Empty;
                    break;

                case FunctionCallContent callContent:
                    result.Role = "assistant";
                    result.ToolCalls ??= [];
                    result.ToolCalls.Add(new NvidiaToolCall
                    {
                        Id = callContent.CallId,
                        Type = "function",
                        Function = new NvidiaFunctionCall
                        {
                            Name = callContent.Name,
                            Arguments = JsonSerializer.Serialize(
                                callContent.Arguments,
                                JsonOptions)
                        }
                    });
                    break;
            }
        }

        if (text.Length > 0)
            result.Content = text.ToString();

        return result;
    }

    private static ChatResponse ConvertResponse(NvidiaResponse response)
    {
        var choice = response.Choices?.FirstOrDefault();

        if (choice is null)
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty));

        var contents = new List<AIContent>();

        if (!string.IsNullOrEmpty(choice.Message?.Content))
            contents.Add(new TextContent(choice.Message.Content));

        if (choice.Message?.ToolCalls is { Count: > 0 } toolCalls)
        {
            foreach (var toolCall in toolCalls)
            {
                var arguments = ParseArguments(
                    toolCall.Function?.Arguments);

                contents.Add(
                    new FunctionCallContent(
                        toolCall.Id ?? Guid.NewGuid().ToString("N"),
                        toolCall.Function?.Name ?? string.Empty,
                        arguments));
            }
        }

        var message = new ChatMessage(
            ChatRole.Assistant,
            contents);

        var result = new ChatResponse(message)
        {
            ModelId = response.Model
        };

        if (response.Usage is not null)
        {
            result.Usage = new UsageDetails
            {
                InputTokenCount = response.Usage.PromptTokens,
                OutputTokenCount = response.Usage.CompletionTokens,
                TotalTokenCount = response.Usage.TotalTokens
            };
        }

        return result;
    }

    private static IEnumerable<ChatResponseUpdate> ConvertStreamingResponse(
        NvidiaResponse response)
    {
        var choice = response.Choices?.FirstOrDefault();

        if (choice?.Delta is null)
            yield break;

        var delta = choice.Delta;

        if (!string.IsNullOrEmpty(delta.Content))
        {
            yield return new ChatResponseUpdate(
                ChatRole.Assistant,
                delta.Content)
            {
                ModelId = response.Model
            };
        }

        if (delta.ToolCalls is { Count: > 0 })
        {
            foreach (var toolCall in delta.ToolCalls)
            {
                if (toolCall.Function?.Name is not null)
                {
                    yield return new ChatResponseUpdate(
                        ChatRole.Assistant,
                        [
                            new FunctionCallContent(
                                toolCall.Id ?? Guid.NewGuid().ToString("N"),
                                toolCall.Function.Name,
                                ParseArguments(toolCall.Function.Arguments))
                        ])
                    {
                        ModelId = response.Model
                    };
                }
            }
        }
    }

    private static Dictionary<string, object?> ParseArguments(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(
                       json,
                       JsonOptions)
                   ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(
        JsonSerializerDefaults.Web);

    private sealed class NvidiaRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<NvidiaMessage> Messages { get; set; } = [];
        public bool Stream { get; set; }
        public double? Temperature { get; set; }
        public double? TopP { get; set; }
        public int? MaxTokens { get; set; }
        public List<NvidiaTool>? Tools { get; set; }
        public string? ToolChoice { get; set; }
    }

    private sealed class NvidiaMessage
    {
        public string Role { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ToolCallId { get; set; }
        public List<NvidiaToolCall>? ToolCalls { get; set; }
    }

    private sealed class NvidiaTool
    {
        public string Type { get; set; } = "function";
        public NvidiaFunction Function { get; set; } = new();
    }

    private sealed class NvidiaFunction
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public JsonElement Parameters { get; set; }
    }

    private sealed class NvidiaToolCall
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public NvidiaFunctionCall? Function { get; set; }
    }

    private sealed class NvidiaFunctionCall
    {
        public string? Name { get; set; }
        public string? Arguments { get; set; }
    }

    private sealed class NvidiaResponse
    {
        public string? Id { get; set; }
        public string? Model { get; set; }
        public List<NvidiaChoice>? Choices { get; set; }
        public NvidiaUsage? Usage { get; set; }
    }

    private sealed class NvidiaChoice
    {
        public NvidiaMessage? Message { get; set; }
        public NvidiaDelta? Delta { get; set; }
        public string? FinishReason { get; set; }
    }

    private sealed class NvidiaDelta
    {
        public string? Content { get; set; }
        public List<NvidiaToolCall>? ToolCalls { get; set; }
    }

    private sealed class NvidiaUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}