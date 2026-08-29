# 🌦 Weather AI Agent

A modular AI-powered weather assistant built with **Microsoft Agent Framework concepts**, **OpenAI-compatible LLM APIs**, and **WeatherAPI**.

The project demonstrates how to build an AI agent with a middleware pipeline for **cross-cutting concerns**, including validation, rate limiting, retries, logging, telemetry, correlation tracking, token usage monitoring, and tool calling.

The architecture follows a familiar **ASP.NET Core middleware pipeline pattern**, where each middleware can perform pre-processing, delegate execution to the next component, and perform post-processing.

---

# Features

- 🤖 AI-powered weather assistant

- 🌍 Current weather retrieval using WeatherAPI

- 🧠 OpenAI-compatible LLM integration

- 🔧 Function/tool calling

- 🔄 Middleware-based agent execution pipeline

- 🛡 Guard middleware

- 🧹 Input sanitization

- 🚦 Rate limiting

- 🔁 Retry handling for transient failures

- ✅ LLM output validation

- 📊 Agent telemetry

- 📝 Structured logging

- 🔗 Correlation IDs

- 🎯 Token usage tracking

- 💰 Estimated token cost tracking

- 💉 Dependency Injection

- ⚙️ Configurable LLM providers

- 🔌 Support for custom OpenAI-compatible endpoints

---

# Architecture

The agent uses a middleware pipeline inspired by the ASP.NET Core request pipeline.

```text
User
 │
 ▼
Agent Pipeline
 │
 ├── CorrelationMiddleware
 ├── LoggingMiddleware
 ├── GuardMiddleware
 ├── RateLimitMiddleware
 ├── InputSanitizationMiddleware
 ├── RetryMiddleware
 ├── AgentTelemetryMiddleware
 ├── OutputValidationMiddleware
 ├── TokenUsageMiddleware
 │
 ▼
AgentService
 │
 ├── Build prompt
 │
 ├── LLM request
 │      │
 │      └── Tool Calling
 │              │
 │              ▼
 │        GetCurrentWeather
 │              │
 │              ▼
 │        WeatherApiService
 │              │
 │              ▼
 │          WeatherAPI
 │
 └── Final LLM response
```

The middleware pipeline keeps cross-cutting concerns outside the core agent logic.

`AgentService` is responsible for the agent workflow itself:

1. Build the system and user messages.

2. Send the request to the configured LLM.

3. Process tool calls returned by the model.

4. Retrieve weather information from WeatherAPI.

5. Return tool results to the LLM.

6. Generate the final response.

7. Store token usage information in the `AgentContext`.

---

# Project Structure

```text
WeatherAIAgent
│
├── Helpers
│   └── WeatherHelper.cs
│
├── Middleware
│   ├── AgentTelemetryMiddleware.cs
│   ├── CorrelationMiddleware.cs
│   ├── GuardMiddleware.cs
│   ├── IAgentMiddleware.cs
│   ├── InputSanitizationMiddleware.cs
│   ├── LoggingMiddleware.cs
│   ├── OutputValidationMiddleware.cs
│   ├── RateLimitMiddleware.cs
│   ├── RetryMiddleware.cs
│   └── TokenUsageMiddleware.cs
│
├── Models
│   ├── AgentContext.cs
│   ├── LLMOptions.cs
│   ├── OpenAIOptions.cs
│   ├── NvidiaOptions.cs
│   ├── WeatherApiOptions.cs
│   ├── WeatherInfo.cs
│   └── TokenUsageInfo.cs
│
├── Services
│   ├── AgentPipeline.cs
│   ├── AgentService.cs
│   ├── ChatClientFactory.cs
│   ├── IWeatherService.cs
│   └── WeatherApiService.cs
│
├── Program.cs
├── ServiceCollectionExtensions.cs
└── appsettings.json
```

---

# Middleware Pipeline

Every agent request passes through the configured middleware pipeline.

Middleware can execute logic **before** and **after** the next component in the pipeline.

This makes it possible to add cross-cutting functionality without modifying the core `AgentService`.

---

## CorrelationMiddleware

Creates a unique correlation ID for every agent request.

The correlation ID is propagated through the pipeline and used to associate logs and telemetry with a single agent execution.

Example:

```text
CorrelationId=67d3ce74-63c5-49da-bab7-726ba0800bfa
```

---

## LoggingMiddleware

Logs the beginning and completion of agent execution.

It records information such as:

- correlation ID

- input length

- execution failures

- exception information

This provides an end-to-end view of the agent request.

---

## GuardMiddleware

Performs basic request validation before the request reaches the agent.

It prevents invalid requests from entering the LLM processing pipeline.

---

## RateLimitMiddleware

Limits the number of agent requests within a configured period.

This helps protect the application and the underlying LLM provider from excessive requests.

---

## InputSanitizationMiddleware

Normalizes and sanitizes user input before it is passed to the agent.

This keeps input processing separate from the agent's business logic.

---

## RetryMiddleware

Retries operations that fail because of transient errors such as temporary service failures or network problems.

Permanent client-side errors are not treated as retryable failures.

---

## AgentTelemetryMiddleware

Collects execution metrics for the complete agent request.

Example metrics include:

- execution duration

- request volume

- failures

- correlation ID

- input length

---

## TokenUsageMiddleware

Tracks token consumption for each agent request.

`AgentService` stores token usage information in `AgentContext`, while the middleware reads and logs the aggregated values.

Tracked values include:

- input tokens

- output tokens

- total tokens

- model

- estimated cost

Estimated cost can be calculated using the `PRICE_PER_1K_TOKENS` environment variable.

---

## OutputValidationMiddleware

Validates the final LLM response against the weather data returned by WeatherAPI.

The middleware helps detect cases where the LLM generates weather information for a location different from the one requested or returned by the weather service.

Example:

```text
WeatherAPI result:

Location:
Dniprorudne
```

Incorrect LLM response:

```text
Weather in Dnipro...
```

Validation:

```text
Failed
```

---

# Tool Calling

The agent uses an LLM function tool named:

```text
GetCurrentWeather
```

The model receives the tool definition and decides when weather information is required.

Example tool schema:

```json
{
  "name": "GetCurrentWeather",
  "description": "Get current weather by city name",
  "parameters": {
    "type": "object",
    "properties": {
      "location": {
        "type": "string",
        "description": "City name"
      }
    },
    "required": [
      "location"
    ]
  }
}
```

The execution flow is:

```text
User
 │
 ▼
LLM
 │
 │ tool call
 ▼
GetCurrentWeather
 │
 ▼
WeatherApiService
 │
 ▼
WeatherAPI
 │
 ▼
Tool result
 │
 ▼
LLM
 │
 ▼
Final response
```

The model is instructed not to guess weather information and to use the weather tool whenever weather data is required.

---

# Weather API

Current weather information is retrieved from **WeatherAPI**.

The service provides:

- Location

- Region

- Country

- Coordinates

- Time zone

- Local time

- Temperature

- Feels-like temperature

- Weather condition

- Humidity

- Wind speed

- Wind gust

- Wind direction

- Visibility

- Atmospheric pressure

- Precipitation

- UV index

- Cloud coverage

- Air quality

- PM2.5

- PM10

The weather service is isolated behind the `IWeatherService` abstraction.

---

# Supported LLM Providers

The application is designed around **OpenAI-compatible chat APIs**.

Currently supported configurations include:

- OpenAI

- NVIDIA NIM

- Other OpenAI-compatible endpoints

The `ChatClientFactory` selects the provider based on configuration and creates the corresponding `ChatClient`.

This allows the agent implementation to remain independent of the underlying LLM provider.

Example:

```text
LLM Provider
     │
     ├── OpenAI
     │
     ├── NVIDIA NIM
     │
     └── Custom OpenAI-compatible API
```

---

# NVIDIA NIM

The project can use NVIDIA NIM through its OpenAI-compatible API.

Example configuration:

```json
{
  "LLM": {
    "Provider": "Nvidia"
  },
  "nVidia": {
    "ApiKey": "<YOUR_NVIDIA_API_KEY>",
    "Model": "openai/gpt-oss-20b",
    "Endpoint": "https://integrate.api.nvidia.com/v1"
  }
}
```

The NVIDIA endpoint follows the OpenAI-compatible Chat Completions API.

The model can therefore be accessed through the same `OpenAI.Chat.ChatClient` abstraction used by the application.

---

# Configuration

Example configuration:

```json
{
  "LLM": {
    "Provider": "Nvidia"
  },

  "OpenAI": {
    "ApiKey": "<YOUR_OPENAI_API_KEY>",
    "Model": "gpt-4o"
  },

  "nVidia": {
    "ApiKey": "<YOUR_NVIDIA_API_KEY>",
    "Model": "openai/gpt-oss-20b",
    "Endpoint": "https://integrate.api.nvidia.com/v1"
  },

  "WeatherApi": {
    "ApiKey": "<YOUR_WEATHERAPI_KEY>",
    "BaseUrl": "https://api.weatherapi.com/v1",
    "AirQuality": true,
    "Language": "en"
  }
}
```

API keys should not be committed to source control.

For local development, environment variables or user secrets can be used instead.

---

# Running the Project

Restore dependencies:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

Run the application:

```bash
dotnet run
```

The application prompts for a city name:

```text
Enter a city name to get the weather forecast or press Esc to exit:
```

Example:

```text
kyiv
```

---

# Example

User:

```text
What's the weather in London?
```

The LLM invokes:

```text
GetCurrentWeather("London")
```

The weather service retrieves the current conditions from WeatherAPI.

The agent then returns the weather information to the user.

Example output:

```text
Current weather information:

Location:   London
Region:     City of London, Greater London
Country:    United Kingdom
Latitude:   51.5171
Longitude:  -0.1062

Temperature:
- Current temperature: 21 °C
- Feels like: 22 °C

Weather condition:
Sunny

Atmospheric conditions:
- Humidity: 64%
- Pressure: 1017 hPa
- Cloud coverage: 0%

Wind conditions:
- Wind speed: 18 km/h
- Wind gust: 25 km/h
- Wind direction: SW
```

---

# Error Handling and Resilience

The agent separates transient infrastructure failures from application-level validation errors.

The middleware pipeline provides several resilience mechanisms:

```text
Request
  │
  ▼
Validation
  │
  ▼
Rate Limiting
  │
  ▼
Retry
  │
  ▼
LLM / Tools
  │
  ▼
Output Validation
  │
  ▼
Telemetry
```

Exceptions are allowed to propagate through the agent service so that the retry and exception middleware can handle them consistently.

---

# Token Usage Tracking

Token usage is collected from LLM responses and stored in the `AgentContext`.

For requests involving multiple LLM calls, such as tool calling, usage is accumulated across the request.

Example:

```text
Input tokens:  420
Output tokens: 180
Total tokens:  600
Estimated cost: 0.0012
```

The exact cost depends on the configured `PRICE_PER_1K_TOKENS` value.

---

# Technologies

- .NET

- C#

- Microsoft Agent Framework concepts

- OpenAI `ChatClient`

- OpenAI-compatible APIs

- NVIDIA NIM

- WeatherAPI

- Function / Tool Calling

- Dependency Injection

- HttpClient

- Middleware Pipeline

- Configuration / Options Pattern

- Structured Logging

- Async/Await

---

# Design Goals

The project focuses on demonstrating several architectural ideas relevant to AI application development:

### Separation of concerns

The agent handler focuses on LLM orchestration and tool execution, while middleware handles cross-cutting concerns.

### Provider independence

The agent is not tightly coupled to a single LLM provider.

### Composable middleware

New cross-cutting functionality can be added as another middleware component without modifying the agent implementation.

### Observability

Correlation IDs, logging, telemetry, and token usage provide visibility into individual agent executions.

### Resilience

Transient LLM or network failures can be handled independently from the core agent logic.

### Validation

Both user input and generated output can be validated independently from the agent implementation.

---

# Future Improvements

- Streaming responses

- Conversation memory

- Multi-turn conversations

- Weather forecast support

- Severe weather alerts

- More weather providers

- More LLM providers

- Circuit breaker middleware

- Caching middleware

- Content safety middleware

- Unit tests

- Integration tests

- End-to-end tests

- OpenTelemetry exporters

- Semantic Kernel integration

---

# License

MIT License

---

# Author

Oleksii Konovalenko

[aldev@ukr.net](mailto:aldev@ukr.net)
