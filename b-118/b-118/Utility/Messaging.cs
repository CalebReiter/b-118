using DSharpPlus.CommandsNext;
using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus;

namespace b_118.Utility
{
    class Messaging
    {
        static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(10);
        static readonly bool DEFAULT_DELETE = true;

        public static Func<string, Task<DiscordMessage>> RespondContent(CommandContext ctx)
        {
            return RespondContent(ctx, DEFAULT_DELETE, DEFAULT_TIMEOUT);
        }

        public static Func<string, Task<DiscordMessage>> RespondContent(CommandContext ctx, bool delete = true)
        {
            return RespondContent(ctx, delete, DEFAULT_TIMEOUT);
        }
        public static Func<string, Task<DiscordMessage>> RespondContent(CommandContext ctx, TimeSpan timeout)
        {
            return RespondContent(ctx, DEFAULT_DELETE, timeout);
        }

        public static Func<string, Task<DiscordMessage>> RespondContent(CommandContext ctx, bool delete, TimeSpan timeout)
        {
            return async (content) =>
            {
                var response = await ctx.RespondAsync(content);
                await DeleteMessages(ctx, response, delete, timeout);
                return response;
            };
        }
        public static Func<string, Task<DiscordMessage>> RespondTTS(CommandContext ctx)
        {
            return RespondTTS(ctx, DEFAULT_DELETE, DEFAULT_TIMEOUT);
        }

        public static Func<string, Task<DiscordMessage>> RespondTTS(CommandContext ctx, bool delete = true)
        {
            return RespondTTS(ctx, delete, DEFAULT_TIMEOUT);
        }

        public static Func<string, Task<DiscordMessage>> RespondTTS(CommandContext ctx, TimeSpan timeout)
        {
            return RespondTTS(ctx, DEFAULT_DELETE, timeout);
        }

        public static Func<string, Task<DiscordMessage>> RespondTTS(CommandContext ctx, bool delete, TimeSpan timeout)
        {
            return async (content) =>
            {
                var response = await ctx.RespondAsync(content, true);
                await DeleteMessages(ctx, response, delete, timeout);
                return response;
            };
        }

        public static Func<string, DiscordEmbed, Task<DiscordMessage>> RespondEmbed(CommandContext ctx)
        {
            return RespondEmbed(ctx, DEFAULT_DELETE, DEFAULT_TIMEOUT);
        }

        public static Func<string, DiscordEmbed, Task<DiscordMessage>> RespondEmbed(CommandContext ctx, bool delete = true)
        {
            return RespondEmbed(ctx, delete, DEFAULT_TIMEOUT);
        }
        public static Func<string, DiscordEmbed, Task<DiscordMessage>> RespondEmbed(CommandContext ctx, TimeSpan timeout)
        {
            return RespondEmbed(ctx, DEFAULT_DELETE, timeout);
        }

        public static Func<string, DiscordEmbed, Task<DiscordMessage>> RespondEmbed(CommandContext ctx, bool delete, TimeSpan timeout)
        {
            return async (content, embed) =>
            {
                var response = await ctx.RespondAsync(content, false, embed);
                await DeleteMessages(ctx, response, delete, timeout);
                return response;
            };
        }

        public static Func<string, DiscordEmbed, Task<DiscordMessage>> RespondTTSEmbed(CommandContext ctx)
        {
            return RespondTTSEmbed(ctx, DEFAULT_DELETE, DEFAULT_TIMEOUT);
        }

        public static Func<string, DiscordEmbed, Task<DiscordMessage>> RespondTTSEmbed(CommandContext ctx, bool delete = true)
        {
            return RespondTTSEmbed(ctx, delete, DEFAULT_TIMEOUT);
        }
        public static Func<string, DiscordEmbed, Task<DiscordMessage>> RespondTTSEmbed(CommandContext ctx, TimeSpan timeout)
        {
            return RespondTTSEmbed(ctx, DEFAULT_DELETE, timeout);
        }

        public static Func<string, DiscordEmbed, Task<DiscordMessage>> RespondTTSEmbed(CommandContext ctx, bool delete, TimeSpan timeout)
        {
            return async (content, embed) =>
            {
                var response = await ctx.RespondAsync(content, true, embed);
                await DeleteMessages(ctx, response, delete, timeout);
                return response;
            };
        }

        private static async Task DeleteMessages(CommandContext ctx, DiscordMessage response, bool delete, TimeSpan timeout)
        {
            try
            {
                if (delete)
                {
                    await ctx.Message.DeleteAsync();
                }
                if (timeout.TotalMilliseconds == 0)
                {
                    await response.DeleteAsync();
                }
                else if (timeout.TotalMilliseconds > 0)
                {
                    await Task.Delay(timeout).ContinueWith(async (_) =>
                    {
                        await response.DeleteAsync();
                    });
                }
            } catch (Exception ex)
            {
                ctx.Client.DebugLogger.LogMessage(LogLevel.Error, "B-118", "", DateTime.Now, ex);
            }
        }
    }
}
