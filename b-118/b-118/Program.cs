using DSharpPlus;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace b_118
{
    class Program
    {
        static DiscordClient discord;
        static string token;
        static DSharpPlus.LogLevel logLevel;

        static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production";

            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            token = configuration.GetSection("Token")["B-118"];
            switch (configuration.GetSection("Log")["Level"])
            {
                case "Debug":
                    {
                        logLevel = DSharpPlus.LogLevel.Debug;
                        break;
                    }
                case "Info":
                    {
                        logLevel = DSharpPlus.LogLevel.Info;
                        break;
                    }
                case "Warning":
                    {
                        logLevel = DSharpPlus.LogLevel.Warning;
                        break;
                    }
                case "Error":
                    {
                        logLevel = DSharpPlus.LogLevel.Error;
                        break;
                    }
                case "Critical":
                    {
                        logLevel = DSharpPlus.LogLevel.Critical;
                        break;
                    }
                default:
                    {
                        logLevel = DSharpPlus.LogLevel.Info;
                        break;
                    }
            }

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole());
        }

        static async Task MainAsync(string[] args)
        {
            discord = new DiscordClient(new DiscordConfiguration
            {
                UseInternalLogHandler = true,
                LogLevel = logLevel,
                Token = token,
                TokenType = TokenType.Bot
            });

            discord.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().StartsWith("ping"))
                    await e.Message.RespondAsync("pong");
            };
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
