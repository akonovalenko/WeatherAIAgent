# 🌦 Weather AI Agent

A modular AI-powered weather assistant built with **Microsoft Agent Framework**, **OpenAI-compatible LLMs**, and **WeatherAPI**.

The project demonstrates how to build a production-ready AI agent using a middleware pipeline, tool calling, retry logic, validation, telemetry, and dependency injection.

---

## Features

* 🤖 AI weather assistant
* 🌍 Current weather retrieval using WeatherAPI
* 🧠 LLM integration (OpenAI-compatible providers)
* 🔄 Middleware-based processing pipeline
* ✅ Output validation
* 🔁 Retry middleware
* 🛡 Guard middleware
* 🧹 Input sanitization
* 📊 Telemetry collection
* 📝 Logging
* 🔗 Correlation IDs
* ⚡ Dependency Injection
* ⚙ Configurable providers

---

# Architecture

```
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
 │
 ▼
AgentService
 │
 ▼
LLM
 │
 ├── Tool Calling
 │
 ▼
WeatherApiService
 │
 ▼
WeatherAPI
```

---

# Project Structure

```
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
│   ├── TokenUsageMiddleware.cs
│   └── RetryMiddleware.cs
│
├── Models
│   ├── AgentContext.cs
│   ├── LLMOptions.cs
│   ├── OpenAIOptions.cs
│   ├── nVidiaOptions.cs
│   ├── WeatherApiOptions.cs
│   ├── WeatherInfo.cs
│   └── TokenUsageInfo.cs
│
├── Services
│   ├── AgentPipeline.cs
│   ├── AgentService.cs
│   ├── ChatClientFactory.cs
│   ├── WeatherApiService.cs
│   └── IWeatherService.cs
│
├── Program.cs
├── ServiceCollectionExtensions.cs
└── appsettings.json
```

---

# Middleware Pipeline

Every request passes through a configurable middleware pipeline.

## CorrelationMiddleware

Creates a unique correlation identifier for every request.

---

## LoggingMiddleware

Logs request execution and responses.

---

## GuardMiddleware

Performs basic request validation.

---

## RateLimitMiddleware

Protects the agent from excessive requests.

---

## InputSanitizationMiddleware

Sanitizes user input before sending it to the language model.

---

## RetryMiddleware

Automatically retries transient failures.

---

## AgentTelemetryMiddleware

Collects execution metrics and token usage.

---

## TokenUsageMiddleware

Reads per-request token usage saved by the agent service and logs a concise summary. The middleware prints a short console line with the number of tokens used for the current request and the aggregated total across the process, as well as estimated cost when `PRICE_PER_1K_TOKENS` is configured. It reads `context.Items["TokenUsage"]` and numeric keys such as `TotalTokensPerRequest`, `EstimatedCostPerRequest`, `TotalTokens`, and `TotalEstimatedCost`.

---

## OutputValidationMiddleware

Validates that the generated response matches the expected weather location returned by WeatherAPI.

This middleware helps prevent hallucinations where the LLM generates weather information for the wrong city.

---

# Weather API

Current weather information is retrieved from WeatherAPI.

The service returns:

* Location
* Region
* Country
* Coordinates
* Local time
* Temperature
* Feels like
* Humidity
* Wind
* Visibility
* Pressure
* UV index
* Cloud coverage
* Air quality
* PM2.5
* PM10

---

# Supported LLM Providers

The project is designed to work with OpenAI-compatible providers.

Examples include:

* OpenAI
* NVIDIA NIM
* Any OpenAI-compatible endpoint

---

# Configuration

Example configuration:

```json
{
  "WeatherApi": {
    "BaseUrl": "http://api.weatherapi.com/v1",
    "ApiKey": "<YOUR_API_KEY>",
    "Language": "en",
    "AirQuality": true
  }
}
```

---

# Running the Project

Restore packages

```
dotnet restore
```

Build

```
dotnet build
```

Run

```
dotnet run
```

---

# Example

User:

```
What's the weather in London?
```

Agent:

```
Current weather in London

Temperature: 21°C
Feels like: 22°C

Humidity: 64%

Wind:
18 km/h SW

Pressure:
1017 mb

Visibility:
10 km

Condition:
Sunny
```

---

# Output Validation

The project contains an output validation middleware that verifies the language model generated the response for the same location returned by WeatherAPI.

Example:

```
WeatherAPI

Location:
Dniprorudne
```

Incorrect LLM response:

```
Weather in Dnipro...
```

Validation:

```
Failed
```

---

# Technologies

* .NET
* C#
* Microsoft Agent Framework
* OpenAI Compatible APIs
* WeatherAPI
* Dependency Injection
* HttpClient
* Middleware Pipeline

---

# Future Improvements

* Streaming responses
* Conversation memory
* Weather forecast support
* Severe weather alerts
* Localization
* Unit tests
* Integration tests
* Multiple weather providers
* Semantic Kernel integration

---

# License

MIT License

---

# Author

aldev@ukr.net
