﻿using DSharpPlus;
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
using b_118.Models;
using b_118.Utility;
using b_118.Database;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;

namespace b_118
{
  class Program
  {
    static DiscordClient discord;
    static string token;
    static string lavalinkPassword;
    static string lavalinkHostname;
    static int lavalinkPort;
    static LogLevel logLevel = LogLevel.Information;
    static CommandsNextExtension commands;
    static InteractivityExtension interactivity;
    static LavalinkExtension lavalink;
    public static string[] Prefixes = new string[] { "b.", "b-beat", "b-campaign", "b-sound" };
    private static IConfiguration configuration;
    private static B118DB b118DB;
    private static SoundClip b118SoundClip;

    static void Main(string[] args)
    {
      string environmentName = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production";
      IConfigurationBuilder builder = new ConfigurationBuilder()
          .AddJsonFile($"appsettings.json", true, true)
          .AddJsonFile($"appsettings.{environmentName}.json", true, true)
          .AddEnvironmentVariables();

      configuration = builder.Build();
      token = configuration.GetSection("Token")["B-118"];
      lavalinkHostname = configuration.GetSection("Lavalink")["Hostname"];
      lavalinkPort = int.Parse(configuration.GetSection("Lavalink")["Port"]);
      lavalinkPassword = configuration.GetSection("Lavalink")["Password"];
      b118DB = new B118DB(configuration.GetSection("Database")["B-118-File"]);
      b118SoundClip = new SoundClip(
          configuration.GetSection("SoundClips")["directory"],
          configuration.GetSection("SoundClips")["regex"]
          );
      Enum.TryParse(configuration.GetSection("Log")["Level"], out logLevel);
      MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    static async Task MainAsync(string[] args)
    {
      await b118DB.Init();
      DiscordConfiguration discordConfiguration = new DiscordConfiguration
      {
        MinimumLogLevel = logLevel,
        Token = token,
        TokenType = TokenType.Bot
      };

      discord = new DiscordClient(discordConfiguration);

      discord.Ready += async (DiscordClient client, ReadyEventArgs e) =>
      {
        await discord.UpdateStatusAsync(new DiscordActivity("The Bee Movie | b.help", ActivityType.Watching));
      };

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
      discord.GuildAvailable += async (DiscordClient client, GuildCreateEventArgs e) =>
      {
        GuildDetails.AddClientGuild(client, e.Guild);
      };

      discord.MessageCreated += async (DiscordClient client, MessageCreateEventArgs e) =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
      {
        if (e.Author.IsBot)
        {
          e.Handled = true;
        }
      };
      discord.MessageCreated += BeeReaction;
      discord.MessageCreated += Bees;
      discord.MessageCreated += Beep;

      discord.ClientErrored += ClientErrored;

      CommandsNextConfiguration commandsNextConfiguration = new CommandsNextConfiguration
      {
        StringPrefixes = Prefixes,
        EnableDms = true,
        EnableDefaultHelp = false
      };
      interactivity = discord.UseInteractivity(new InteractivityConfiguration()
      {
        Timeout = TimeSpan.FromSeconds(30)
      });

      commands = discord.UseCommandsNext(commandsNextConfiguration);
      commands.CommandExecuted += CommandExecuted;
      commands.CommandErrored += CommandErrored;
      commands.RegisterCommands<BoardCommands>();
      commands.RegisterCommands<BeatCommands>();
      commands.RegisterCommands<CampaignCommands>();
      commands.RegisterCommands<BCommands>();

      lavalink = discord.UseLavalink();

      await discord.ConnectAsync();
      await Task.Delay(-1);
    }

    public static B118DB GetB118DB()
    {
      return b118DB;
    }

    public static SoundClip GetB118SoundClip()
    {
      return b118SoundClip;
    }

    public static Colors GetColors()
    {
      return new Colors(configuration);
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

    private static async Task Beep(DiscordClient client, MessageCreateEventArgs e)
    {
      if (e.Message.Content.ToLower() == "beep")
      {
        await e.Message.RespondAsync("boop");
      }
    }

    private static async Task Bees(DiscordClient client, MessageCreateEventArgs e)
    {
      if (e.Message.Content.ToLower() == "bees?")
      {
        await e.Message.RespondAsync("According to all known laws of aviation,"
                                   + " there is no way a bee should be able to fly."
                                   + " Its wings are too small to get its fat little body off the ground.", true);
        await e.Message.RespondAsync("The bee, of course, flies anyway"
                                   + " because bees don't care what humans think is impossible.", true);
        await e.Message.RespondAsync("Yellow, black. Yellow, black. Yellow, black. Yellow, black."
                                   + " Ooh, black and yellow!"
                                   + " Let's shake it up a little.", true);
      }
    }

    private static async Task BeeReaction(DiscordClient client, MessageCreateEventArgs e)
    {
      if (e.Message.Content.Contains(new char[] { 'b', 'B' }))
      {
        if (!e.Channel.IsPrivate)
        {
          GuildDetails guildDetails = GuildDetails.ClientGuilds[e.Guild.Id];
          if (!guildDetails.GetCooldown("beereaction"))
          {
            await e.Message.CreateReactionAsync(DiscordEmoji.FromName(discord, ":bee:"));
            guildDetails.SetCooldown("beereaction", TimeSpan.FromMinutes(5));
          }
        }
      }
    }

    private static Task ClientErrored(DiscordClient client, ClientErrorEventArgs e)
    {
      client.Logger.Log(LogLevel.Error, "B-118", "Exception occured", DateTime.Now, e.Exception);
      return Task.CompletedTask;
    }

    private static Task CommandExecuted(CommandsNextExtension next, CommandExecutionEventArgs e)
    {
      e.Context.Client.Logger.Log(LogLevel.Information, "B-118", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

      return Task.CompletedTask;
    }

    private static async Task CommandErrored(CommandsNextExtension next, CommandErrorEventArgs e)
    {
      e.Context.Client.Logger.Log(LogLevel.Error, "B-118", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored.", DateTime.Now, e.Exception);

      if (e.Exception is ChecksFailedException)
      {
        DiscordEmoji emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

        DiscordEmbedBuilder embed = new DiscordEmbedBuilder
        {
          Title = "Access denied",
          Description = $"{emoji} You do not have the permissions required to execute this command.",
          Color = new DiscordColor(0xFF0101) // red
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
