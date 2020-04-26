using DSharpPlus;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DSharpPlus.CommandsNext;
using b_118.Commands;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;

namespace b_118
{
    class Program
    {
        static DiscordClient discord;
        static string token;
        static string lavalinkPassword;
        static string lavalinkHostname;
        static int lavalinkPort;
        static LogLevel logLevel;
        static CommandsNextExtension commands;
        static LavalinkExtension lavalink;

        static void Main(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production";

            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.{environmentName}.json", true, true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            token = configuration.GetSection("Token")["B-118"];
            lavalinkHostname = configuration.GetSection("Lavalink")["Hostname"];
            lavalinkPort = int.Parse(configuration.GetSection("Lavalink")["Port"]);
            lavalinkPassword = configuration.GetSection("Lavalink")["Password"];
            switch (configuration.GetSection("Log")["Level"])
            {
                case "Debug":
                    {
                        logLevel = LogLevel.Debug;
                        break;
                    }
                case "Info":
                    {
                        logLevel = LogLevel.Info;
                        break;
                    }
                case "Warning":
                    {
                        logLevel = LogLevel.Warning;
                        break;
                    }
                case "Error":
                    {
                        logLevel = LogLevel.Error;
                        break;
                    }
                case "Critical":
                    {
                        logLevel = LogLevel.Critical;
                        break;
                    }
                default:
                    {
                        logLevel = LogLevel.Info;
                        break;
                    }
            }

            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            var discordConfiguration = new DiscordConfiguration
            {
                UseInternalLogHandler = true,
                LogLevel = logLevel,
                Token = token,
                TokenType = TokenType.Bot
            };

            discord = new DiscordClient(discordConfiguration);

            discord.ClientErrored += ClientErrored;

            var commandsNextConfiguration = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] {"b"},
                EnableDms = true,
                EnableMentionPrefix = true,
                EnableDefaultHelp = true
            };

            commands = discord.UseCommandsNext(commandsNextConfiguration);
            commands.CommandExecuted += CommandExecuted;
            commands.CommandErrored += CommandErrored;
            commands.RegisterCommands<BCommands>();
            commands.RegisterCommands<BeatCommands>();

            lavalink = discord.UseLavalink();

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        public static ConnectionEndpoint GetLavalinkConnectionEndpoint()
        {
            return new ConnectionEndpoint { Hostname = lavalinkHostname, Port = lavalinkPort };
        }

        public static LavalinkConfiguration GetLavalinkConfiguration()
        {
            return new LavalinkConfiguration
            {
                RestEndpoint = GetLavalinkConnectionEndpoint(),
                SocketEndpoint = GetLavalinkConnectionEndpoint(),
                Password = lavalinkPassword
            };
        }

        private static Task ClientErrored(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "B-118", "Exception occured", DateTime.Now, e.Exception);

            return Task.CompletedTask;
        }

        private static Task CommandExecuted(CommandExecutionEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "B-118", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            return Task.CompletedTask;
        }

        private static async Task CommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "B-118", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored.", DateTime.Now, e.Exception);

            if (e.Exception is ChecksFailedException ex)
            {
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
                
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync("", embed: embed);
            }
            else if (e.Exception is InvalidOperationException exc)
            {
                await e.Context.RespondAsync(exc.Message);
            }
            else
            {
                await e.Context.RespondAsync("Something went wrong.");
            }
        }
    }
}
