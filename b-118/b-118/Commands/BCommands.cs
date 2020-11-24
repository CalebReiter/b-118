using b_118.Aspects;
using b_118.Utility;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace b_118.Commands
{
  [Description("General commands.")]
  class BCommands : BaseCommandModule
  {
    public readonly CustomPrefix _prefix;
    public readonly Dictionary<string, Type> COMMAND_TYPES = new Dictionary<string, Type>();

    public BCommands() : base()
    {
      _prefix = new CustomPrefix("b.");
      BeatCommands beat = new BeatCommands();
      CampaignCommands campaign = new CampaignCommands();
      BoardCommands board = new BoardCommands();
      COMMAND_TYPES.Add(_prefix._prefix, GetType());
      COMMAND_TYPES.Add(beat._prefix._prefix, beat.GetType());
      COMMAND_TYPES.Add(campaign._prefix._prefix, campaign.GetType());
      COMMAND_TYPES.Add(board._prefix._prefix, board.GetType());
    }

    [Command("quote")]
    [RequirePrefixes("b.")]
    [Description("Quote a user.")]
    public async Task Quote(CommandContext ctx, [Description("ID of message to quote.")] ulong message_id)
    {
            Messaging messaging = new Messaging(ctx);
            IReadOnlyDictionary<ulong, DiscordChannel> channels = ctx.Guild.Channels;
            bool found = false;
            foreach (KeyValuePair<ulong, DiscordChannel> pair in channels)
            {
                DiscordChannel channel = pair.Value;
                try
                {
                    DiscordMessage message = await channel.GetMessageAsync(message_id);
                    DiscordEmbed embed = new DiscordEmbedBuilder()
                        .WithAuthor(message.Author.Username, null, message.Author.GetAvatarUrl(DSharpPlus.ImageFormat.Auto))
                        .WithDescription(message.Content)
                        .WithColor(0xFFFF01)
                        .Build();
                    await messaging.RespondEmbed(true, false)("", embed);
                    found = true;
                    continue;
                } catch (Exception)
                {
                }
            }
            if (!found)
            {
                await messaging.RespondContent(true)("Message was not found.");
            }
        }

    /// <summary>
    /// This command will change in the future. Just testing executing a command from an argument.
    /// Preferably the flow will be that a user will first save an alias with a name,
    /// then they can use this command to execute their saved aliases.
    /// <example>
    /// b.new-alias tf "sound board tavern/table_flip 200 10"
    /// b.alias tf
    /// Executing sound board tavern/table_flip 200 10
    /// </example>
    /// </summary>
    /// <param name="ctx">The CommandContext for the message.</param>
    /// <param name="alias">Alias to execute.</param>
    [Command("alias")]
    [RequirePrefixes("b.")]
    [Description("Execute an alias.")]
    public async Task Alias(CommandContext ctx, [Description("Command to execute.")] string alias)
    {
      string prefix = null;
      foreach (string p in Program.Prefixes)
      {
        if (alias.StartsWith(p))
        {
          prefix = p;
        }
      }
      Type commandType = COMMAND_TYPES[prefix];
      IEnumerable<MethodInfo> methods = commandType.GetMethods().Where(method =>
          method.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0);
      foreach (MethodInfo method in methods)
      {
        CommandAttribute command = (CommandAttribute)method.GetCustomAttributes(typeof(CommandAttribute), false).First();
        // Match starts with command but also isn't a command that starts with a superstring of the given command.
        Regex regex = new Regex($"^{prefix}\\s?{command.Name}(\\s{1}|$)");
        if (regex.Match(alias).Success)
        {
          Command aliasCommand = ctx.CommandsNext.RegisteredCommands[command.Name];
          string commandArguments = alias.Split(command.Name)[1];
          CommandContext context = ctx.CommandsNext.CreateFakeContext(ctx.User, ctx.Channel, alias, prefix, aliasCommand, commandArguments);
          DiscordMessage response = await ctx.RespondAsync($"Executing `{alias}`");
          await ctx.Message.DeleteAsync();
          await context.CommandsNext.ExecuteCommandAsync(context);
          await response.DeleteAsync();
          return;
        }
      }
    }

    /// <summary>
    /// Use reflection to get the Command classes and their descriptions.
    /// </summary>
    /// <param name="ctx">The CommandContext for the message.</param>
    [Command("help")]
    [RequirePrefixes("b.")]
    [Hidden]
    public async Task Help(CommandContext ctx)
    {
      string content = "";
      foreach (KeyValuePair<string, Type> commandType in COMMAND_TYPES)
      {
        DescriptionAttribute description = (DescriptionAttribute)commandType.Value.GetCustomAttributes(typeof(DescriptionAttribute), false).First();
        content += $"\n`{commandType.Key}`\t{description.Description}";
      }
      DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
      {
        Title = "help",
        Description = content,
        Color = Program.GetColors().Yellow
      };
      Messaging messaging = new Messaging(ctx);
      await messaging.RespondEmbed(TimeSpan.FromMinutes(5))(null, embedBuilder.Build());
    }

    /// <summary>
    /// Use reflection to get the commands of a given Command class and their descriptions.
    /// </summary>
    /// <param name="ctx">The CommandContext for the message.</param>
    /// <param name="prefix">The prefix for the Command class</param>
    [Command("help")]
    [RequirePrefixes("b.")]
    [Hidden]
    public async Task Help(CommandContext ctx, string prefix)
    {
      Messaging messaging = new Messaging(ctx);
      Type type = COMMAND_TYPES[prefix];
      if (type == null)
      {
        await messaging.RespondContent()($"There is no command with the {prefix} prefix");
        return;
      }
      DescriptionAttribute commandDescription = (DescriptionAttribute)type.GetCustomAttributes(typeof(DescriptionAttribute), false).First();
      string content = $"{commandDescription.Description}";
      IEnumerable<MethodInfo> methods = type.GetMethods().Where(method =>
          method.GetCustomAttributes(typeof(HiddenAttribute), false).Length <= 0 &&
          method.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0
      );
      foreach (MethodInfo method in methods)
      {
        CommandAttribute command = (CommandAttribute)method.GetCustomAttributes(typeof(CommandAttribute), false).First();
        DescriptionAttribute methodDescription = (DescriptionAttribute)method.GetCustomAttributes(typeof(DescriptionAttribute), false).First();
        content += $"\n`{prefix} {command.Name}`\t{methodDescription.Description}";
      }
      DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
      {
        Title = $"help {prefix}",
        Description = content,
        Color = Program.GetColors().Yellow
      };
      await messaging.RespondEmbed(TimeSpan.FromMinutes(5))(null, embedBuilder.Build());
    }

    /// <summary>
    /// Use reflection to get the parameters of a given Command from a Command class and their descriptions.
    /// </summary>
    /// <param name="ctx">The CommandContext for the message.</param>
    /// <param name="prefix">The prefix for the Command class</param>
    /// <param name="command">The command from the Command class</param>
    [Command("help")]
    [RequirePrefixes("b.")]
    [Description("Get help for commands.")]
    public async Task Help(CommandContext ctx, [Description("The prefix to get help with.")] string prefix, [Description("The command to get help with.")] string command)
    {
      Messaging messaging = new Messaging(ctx);
      Type type = COMMAND_TYPES[prefix];
      if (type == null)
      {
        await messaging.RespondContent()($"There is no command with the {prefix} prefix");
        return;
      }
      MethodInfo method = type.GetMethods().Where(method =>
      {
        try
        {
          CommandAttribute _command = (CommandAttribute)method.GetCustomAttributes(typeof(CommandAttribute), false).First();
          return method.GetCustomAttributes(typeof(HiddenAttribute), false).Length <= 0 &&
                           _command.Name.ToLower().Equals(command.ToLower());
        }
        catch (InvalidOperationException)
        {
          return false;
        }
      }
      ).First();
      DescriptionAttribute methodDescription = (DescriptionAttribute)method.GetCustomAttributes(typeof(DescriptionAttribute), false).First();
      string content = methodDescription.Description;
      IEnumerable<ParameterInfo> parameters = method.GetParameters().Where(parameter =>
          parameter.GetCustomAttributes(typeof(DescriptionAttribute), false).Length > 0
      );
      foreach (ParameterInfo parameter in parameters)
      {
        DescriptionAttribute parameterDescription = (DescriptionAttribute)parameter.GetCustomAttributes(typeof(DescriptionAttribute), false).First();
        string optional = parameter.IsOptional ? "?" : "";
        string defaultValue = (parameter.HasDefaultValue && parameter.DefaultValue != null) ? $"={parameter.DefaultValue}" : "";
        Type[] genericTypes = parameter.ParameterType.GenericTypeArguments;
        Type parameterType = genericTypes.Length > 0 ? genericTypes[0] : parameter.ParameterType;
        string typeName = ctx.CommandsNext.GetUserFriendlyTypeName(parameterType);
        content += $"\n`{parameter.Name}: {typeName}{optional}{defaultValue}`\t{parameterDescription.Description}";
      }
      DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
      {
        Title = $"help {prefix} {command}",
        Description = content,
        Color = Program.GetColors().Yellow
      };
      await messaging.RespondEmbed(TimeSpan.FromMinutes(5))(null, embedBuilder.Build());
    }
  }
}
