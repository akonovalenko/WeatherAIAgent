# WeatherAIAgent

**Product version: 1.3.2**

A console application demonstrating a one-shot AI agent built with **Microsoft.Agents.AI**. The agent accepts a city name, invokes a weather tool, retrieves current weather from WeatherAPI, and returns the authoritative tool output without allowing the LLM to rewrite it.

## Main goals

- Use `AIAgent` as the application-facing abstraction rather than exposing `ChatClient` to the service layer.
- Support OpenAI-compatible providers through a factory.
- Keep external weather access behind `IWeatherService`.
- Demonstrate an application middleware pipeline with validation, correlation, telemetry, LLM health checks, timeout control, rate limiting, exception handling, and token usage reporting.
- Preserve the original weather response as the source of truth.

## Architecture

```text
Program
  -> AgentPipeline
      -> CorrelationMiddleware
      -> LLMHealthMiddleware
      -> AgentTelemetryMiddleware
      -> ExceptionMiddleware
      -> LoggingMiddleware
      -> GuardMiddleware
      -> InputSanitizationMiddleware
      -> RateLimitMiddleware
      -> OutputValidationMiddleware
      -> TokenUsageMiddleware
      -> AgentService
          -> IAIAgentFactory
          -> IWeatherTool
              -> IWeatherService
                  -> WeatherApiService
```

The pipeline is ordered by `IAgentMiddleware.Order`; registration order is not the source of truth. Lower order values execute earlier on the way in. Middleware after the agent service executes on the way out according to the composed delegate chain.

### Responsibilities

| Component | Responsibility |
|---|---|
| `Program` | Console UI and application host startup. |
| `AgentPipeline` | Creates a per-request `AgentContext` and composes middleware. |
| `AgentContext` | Carries request input, cancellation, correlation data, execution metadata, and weather state. |
| `AgentService` | Creates the tool and `AIAgent`, runs the agent, and records token usage. |
| `AIAgentFactory` | Selects the configured provider and constructs the framework agent. Provider-specific client details remain here. |
| `LLMHealthService` | Caches endpoint/model health checks using the OpenAI-compatible `/models` endpoint. |
| `LLMHealthMiddleware` | Performs the cached health check and applies the application-level agent timeout. |
| `WeatherTool` | Exposes the weather operation as an AI function and formats the authoritative result. |
| `WeatherApiService` | Calls WeatherAPI, parses the response, and maps provider failures to application exceptions. |
| Middleware | Implements cross-cutting request concerns without placing them in the domain service. |

## Important execution behavior

1. Input is validated and normalized before the agent is called.
2. The weather tool receives the city from `AgentContext`; the model does not provide a location argument.
3. The tool calls WeatherAPI and stores the returned `WeatherInfo` and formatted output in the context.
4. `AIAgentFactory` uses the Chat Completions adapter for OpenAI-compatible endpoints. This is intentional for providers that do not fully support the Responses API schema.
5. The agent builder intercepts function invocation and sets `context.Terminate = true` after a successful `GetCurrentWeather` call, preventing an unnecessary second inference step.
6. `OutputValidationMiddleware` verifies that the resolved location matches the requested location and that the final output is exactly the formatted tool result.

## Configuration

Configuration is read from `appsettings.json`, environment variables, and .NET user secrets according to the normal Generic Host configuration rules.

Example shape:

```json
{
  "LLM": {
    "Provider": "nVidia",
    "AgentTimeoutSeconds": 120,
    "HealthCheckIntervalSeconds": 30,
    "HealthCheckTimeoutSeconds": 5
  },
  "OpenAI": {
    "ApiKey": "",
    "Endpoint": "https://api.openai.com/v1",
    "Model": ""
  },
  "nVidia": {
    "ApiKey": "",
    "Endpoint": "",
    "Model": ""
  },
  "WeatherApi": {
    "ApiKey": "",
    "BaseUrl": "https://api.weatherapi.com/v1",
    "Language": "en",
    "AirQuality": true,
    "Timeout": "00:00:10"
  }
}
```

Do not commit API keys. Prefer user secrets or environment variables. `LLM_API_KEY` can be used as a fallback when the selected provider key is empty. Token pricing is not estimated unless it is explicitly configured in the application.

## Running

```bash
dotnet restore
dotnet build
dotnet run
```

Enter a city name and press Enter. Press Esc while entering input to exit.

## Middleware order

| Order | Middleware | Purpose |
|---:|---|---|
| 10 | Correlation | Assigns a request correlation ID. |
| 15 | LLM health | Checks endpoint availability and configured model availability. Cached to avoid a health request on every execution. |
| 20 | AgentTelemetry | Measures request duration. |
| 30 | Exception | Converts unhandled exceptions into user-friendly messages. |
| 40 | Logging | Logs request lifecycle events without logging the full prompt. |
| 50 | Guard | Applies basic input validation. |
| 60 | Input sanitization | Removes unsafe control characters and normalizes whitespace. |
| 70 | Rate limit | Limits each user to 10 requests per minute. |
| 90 | Output validation | Verifies authoritative weather output and location consistency. |
| 100 | Token usage | Reports usage after the downstream operation completes. |

There is currently no separate `RetryMiddleware` in the archive. Transient retry behavior is handled by the weather HTTP service/provider configuration rather than by repeating the complete agent run. This avoids duplicating LLM calls and token usage.

`LLMHealthMiddleware` is the single LLM protection layer. It checks the configured provider before agent execution, uses the cached health result, verifies that the configured model appears in `/models`, and applies the application-level execution timeout to the complete downstream agent run. `AgentService` only consumes the cancellation token provided by the pipeline.

There is currently no fallback provider/model. If the configured provider is unhealthy or the model is unavailable, the request fails instead of switching to another provider.

## Architectural review notes

### Strengths

- The application depends on `IAIAgentFactory`, `IWeatherTool`, and `IWeatherService`, which keeps infrastructure details out of the orchestration layer.
- The provider-specific `ChatClient` is isolated in `AIAgentFactory`.
- The weather tool is deterministic and authoritative; the model is used for tool selection and protocol handling rather than for inventing weather data.
- Middleware composition is explicit and testable.
- Cancellation and timeout are propagated to the agent and weather tool.

### Trade-offs and future improvements

- `AgentContext` is intentionally mutable for this demo, but a larger application should separate immutable request data from mutable execution state and use typed context properties instead of a general `Dictionary<string, object>`.
- The factory currently selects a provider at construction time. If provider switching must happen per request, introduce a provider registry or keyed factories rather than mutating shared state.
- `WeatherTool` contains presentation formatting. For a larger system, move formatting into a dedicated formatter or return a structured tool result and format it at the boundary.
- Rate limiting is in-memory and process-local. A multi-instance deployment needs a distributed store or gateway-level rate limiting.
- Retry policies are hand-written. A production service may benefit from a policy library, but retry ownership must remain clear to avoid retrying both the weather call and the entire agent run.
- The console UI is coupled to `Program`; a web/API adapter could reuse the pipeline without changing the core orchestration.

## Limitations

This is a demonstration application, not a production weather platform. It has no persistent conversation memory, authentication, distributed rate limiting, durable telemetry export, or automated test project in the current archive.


## Configuration notes

- The configured provider endpoint and model are validated by `LLMHealthService` before an agent run.
- The same `LLM.AgentTimeoutSeconds` value is used as the application execution limit and the OpenAI-compatible client network timeout.
- Token usage is reported directly from the provider response. No artificial token-price placeholder is used.
- `LLM_API_KEY` remains an explicit environment-variable fallback for provider credentials.
