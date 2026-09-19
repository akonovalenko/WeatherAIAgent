using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WeatherAgent.Middleware;
using WeatherAgent.Services;
using WeatherAgent.Tools;
using WeatherAIAgent.Interfaces;
using WeatherAIAgent.Models;

namespace WeatherAgent.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to add WeatherAgent services.
/// </summary>
/// <Author>Oleksii Konovalenko</Author>
/// <CreatedDate></CreatedDate>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds WeatherAgent services to the IServiceCollection.   
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">The configuration to use.</param>
    /// <returns>The IServiceCollection.</returns>
    public static IServiceCollection AddWeatherAgent(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LLMOptions>(configuration.GetSection(LLMOptions.SectionName));
        services.Configure<OpenAIOptions>(configuration.GetSection(OpenAIOptions.SectionName));
        services.Configure<NvidiaOptions>(configuration.GetSection(NvidiaOptions.SectionName));
        services.Configure<WeatherApiOptions>(configuration.GetSection(WeatherApiOptions.SectionName));
        services.AddSingleton<IAIAgentFactory, AIAgentFactory>();
        services.AddTransient<IWeatherTool, WeatherTool>();
        services.AddHttpClient<IWeatherService, WeatherApiService>(
            (sp, client) => {
                var options = sp.GetRequiredService<IOptions<WeatherApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = options.Timeout;
            });

        // Middleware execution order is defined by IAgentMiddleware.Order.
        // Retry must be inside the exception boundary so transient exceptions can
        // propagate to it before final handling.
        services.AddSingleton<IAgentMiddleware, CorrelationMiddleware>();
        services.AddSingleton<IAgentMiddleware, AgentTelemetryMiddleware>();
        services.AddSingleton<IAgentMiddleware, ExceptionMiddleware>();
        services.AddSingleton<IAgentMiddleware, LoggingMiddleware>();
        services.AddSingleton<IAgentMiddleware, GuardMiddleware>();
        services.AddSingleton<IAgentMiddleware, InputSanitizationMiddleware>();
        services.AddSingleton<IAgentMiddleware, RateLimitMiddleware>();
        services.AddSingleton<IAgentMiddleware, OutputValidationMiddleware>();
        services.AddSingleton<IAgentMiddleware, TokenUsageMiddleware>();

        // AgentService depends on a typed HttpClient service. Keep the request
        // pipeline transient so we do not capture a transient typed client in a singleton.
        services.AddTransient<AgentService>();
        services.AddTransient<AgentPipeline>();

        return services;
    }
}