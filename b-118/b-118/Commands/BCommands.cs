using b_118.Aspects;
using b_118.Utility;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var beat = new BeatCommands();
            var campaign = new CampaignCommands();
            var board = new BoardCommands();
            COMMAND_TYPES.Add(_prefix._prefix, GetType());
            COMMAND_TYPES.Add(beat._prefix._prefix, beat.GetType());
            COMMAND_TYPES.Add(campaign._prefix._prefix, campaign.GetType());
            COMMAND_TYPES.Add(board._prefix._prefix, board.GetType());
        }

        [Command("alias")]
        public async Task Alias(CommandContext ctx, [Description("Command to execute.")] string alias)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                string prefix = null;
                foreach (string s in Program.Prefixes) {
                    if (alias.StartsWith(s))
                    {
                        prefix = s;
                    }
                }
                foreach (var type in COMMAND_TYPES)
                {
                    var methods = type.Value.GetMethods().Where(method =>
                        method.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0);
                    foreach (var method in methods)
                    {
                        var command = (CommandAttribute)method.GetCustomAttributes(typeof(CommandAttribute), false).First();
                        if (alias.StartsWith($"{prefix}{command.Name}") || alias.StartsWith($"{prefix} {command.Name}")) {
                            var _command = ctx.CommandsNext.RegisteredCommands[command.Name];
                            var context = ctx.CommandsNext.CreateFakeContext(ctx.User, ctx.Channel, alias, prefix, _command, alias.Split(command.Name)[1]);
                            DiscordMessage response = await ctx.RespondAsync($"Executing `{alias}`");
                            await ctx.Message.DeleteAsync();
                            await context.CommandsNext.ExecuteCommandAsync(context);
                            await response.DeleteAsync();
                            return;
                        }
                    }
                }
            });
        }

        [Command("help")]
        [Hidden]
        public async Task Help(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                string content = "";
                foreach (var type in COMMAND_TYPES)
                {
                    var attr = (DescriptionAttribute)type.Value.GetCustomAttributes(typeof(DescriptionAttribute), false).First();
                    content += $"\n`{type.Key}`\t{attr.Description}";
                }
                var embed = new DiscordEmbedBuilder()
                {
                    Title = "help",
                    Description = content,
                    Color = Program.GetColors().Yellow
                };
                var messaging = new Messaging(ctx);
                await messaging.RespondEmbed(TimeSpan.FromMinutes(5))(null, embed);
            });
        }

        [Command("help")]
        [Hidden]
        public async Task Help(CommandContext ctx, string prefix)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var messaging = new Messaging(ctx);
                var type = COMMAND_TYPES[prefix];
                if (type == null)
                {
                    await messaging.RespondContent()($"There is no command with the {prefix} prefix");
                    return;
                }
                var description = (DescriptionAttribute)type.GetCustomAttributes(typeof(DescriptionAttribute), false).First();
                string content = $"{description.Description}";
                var methods = type.GetMethods().Where(method =>
                    method.GetCustomAttributes(typeof(HiddenAttribute), false).Length <= 0 &&
                    method.GetCustomAttributes(typeof(CommandAttribute), false).Length > 0
                ).ToArray();
                foreach (var method in methods)
                {
                    var commandAttr = (CommandAttribute)method.GetCustomAttributes(typeof(CommandAttribute), false).First();
                    var descriptionAttr = (DescriptionAttribute)method.GetCustomAttributes(typeof(DescriptionAttribute), false).First();
                    content += $"\n`{prefix} {commandAttr.Name}`\t{descriptionAttr.Description}";
                }
                var embed = new DiscordEmbedBuilder()
                {
                    Title = $"help {prefix}",
                    Description = content,
                    Color = Program.GetColors().Yellow
                };
                await messaging.RespondEmbed(TimeSpan.FromMinutes(5))(null, embed);
            });
        }

        [Command("help")]
        [Description("Get help for commands.")]
        public async Task Help(CommandContext ctx, [Description("The prefix to get help with.")] string prefix, [Description("The command to get help with.")] string command)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var messaging = new Messaging(ctx);
                var type = COMMAND_TYPES[prefix];
                if (type == null)
                {
                    await messaging.RespondContent()($"There is no command with the {prefix} prefix");
                    return;
                }
                var method = type.GetMethods().Where(method =>
                {
                    try
                    {
                        var commandAttr = (CommandAttribute)method.GetCustomAttributes(typeof(CommandAttribute), false).First();
                        return method.GetCustomAttributes(typeof(HiddenAttribute), false).Length <= 0 &&
                        commandAttr.Name.ToLower().Equals(command.ToLower());
                    } catch (InvalidOperationException)
                    {
                        return false;
                    }
                }
                ).Single();
                var description = (DescriptionAttribute)method.GetCustomAttributes(typeof(DescriptionAttribute), false).First();
                string content = description.Description;
                var parameters = method.GetParameters().Where(parameter =>
                    parameter.GetCustomAttributes(typeof(DescriptionAttribute), false).Length > 0
                );
                foreach (var parameter in parameters)
                {
                    var descriptionAttr = (DescriptionAttribute)parameter.GetCustomAttributes(typeof(DescriptionAttribute), false).First();
                    var optional = parameter.IsOptional ? "?" : "";
                    var defaultValue = (parameter.HasDefaultValue && parameter.DefaultValue != null) ? $"={parameter.DefaultValue}" : "";
                    var genericTypes = parameter.ParameterType.GenericTypeArguments;
                    var parameterType= genericTypes.Length > 0 ? genericTypes[0] : parameter.ParameterType;
                    var typeName = ctx.CommandsNext.GetUserFriendlyTypeName(parameterType); 
                    content += $"\n`{parameter.Name}: {typeName}{optional}{defaultValue}`\t{descriptionAttr.Description}";
                }
                var embed = new DiscordEmbedBuilder()
                {
                    Title = $"help {prefix} {command}",
                    Description = content,
                    Color = Program.GetColors().Yellow
                };
                await messaging.RespondEmbed(TimeSpan.FromMinutes(5))(null, embed);
            });
        }
    }
}
