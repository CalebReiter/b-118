using DSharpPlus;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace b_118
{
    class Program
    {
        static DiscordClient discord;
        static string token;

        static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ENV");

            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            token = configuration.GetSection("Token")["B-118"];

            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            discord = new DiscordClient(new DiscordConfiguration
            {
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
