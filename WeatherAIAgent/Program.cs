using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeatherAgent.Extensions;
using WeatherAgent.Services;

namespace WeatherAgent
{
    /// <summary>
    /// The main entry point for the WeatherAgent application.
    /// </summary>
    /// <Author>Oleksii Konovalenko</Author>
    /// <CreatedDate></CreatedDate>
    public static class Program
    {
        /// <summary>
        /// The main method that initializes and runs the WeatherAgent application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddWeatherAgent(builder.Configuration);
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            using var host = builder.Build();
            var pipeline = host.Services.GetRequiredService<AgentPipeline>();

            Console.WriteLine("OpenAI Compatible AI Agent Demo by O.K.");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("aldev@ukr.net");
            Console.ResetColor();

            while (true)
            {
                Console.Write("Enter a city name to get the weather forecast or press Esc to exit: ");
                var input = string.Empty;

                while (true)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine();
                        return;
                    }
                    if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        break;
                    }
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (input.Length > 0)
                        {
                            input = input[..^1];
                            Console.Write("\b \b");
                        }
                        continue;
                    }

                    input += key.KeyChar;
                    Console.Write(key.KeyChar);
                }

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var result = await pipeline.ExecuteAsync(input);

                Console.WriteLine(result);
                Console.WriteLine();
            }
        }
    }
}