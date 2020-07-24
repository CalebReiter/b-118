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
        private readonly DiscordClient _client;
        private readonly DiscordMessage _message;

        public Messaging(DiscordClient client, DiscordMessage message)
        {
            _client = client;
            _message = message;
        }

        public Messaging(CommandContext context)
        {
            _client = context.Client;
            _message = context.Message;
        }

        public Func<string, Task<DiscordMessage>> RespondContent(bool delete = true)
        {
            return RespondContent(delete, DEFAULT_TIMEOUT, DEFAULT_DELETE);
        }
        public Func<string, Task<DiscordMessage>> RespondContent(TimeSpan timeout)
        {
            return RespondContent(DEFAULT_DELETE, timeout, DEFAULT_DELETE);
        }
        public Func<string, Task<DiscordMessage>> RespondContent(bool delete, bool deleteResponse)
        {
            return RespondContent(delete, DEFAULT_TIMEOUT, deleteResponse);
        }

        public Func<string, Task<DiscordMessage>> RespondContent(bool delete, TimeSpan timeout)
        {
            return RespondContent(delete, timeout, DEFAULT_DELETE);
        }

        public Func<string, Task<DiscordMessage>> RespondContent(bool delete, TimeSpan timeout, bool deleteResponse)
        {
            return async (content) =>
            {
                var response = await _message.RespondAsync(content);
                await DeleteMessages(response, delete, timeout, deleteResponse);
                return response;
            };
        }

        public Func<string, Task<DiscordMessage>> RespondTTS(bool delete = true)
        {
            return RespondTTS(delete, DEFAULT_TIMEOUT);
        }

        public Func<string, Task<DiscordMessage>> RespondTTS(TimeSpan timeout)
        {
            return RespondTTS(DEFAULT_DELETE, timeout);
        }

        public Func<string, Task<DiscordMessage>> RespondTTS(bool delete, TimeSpan timeout)
        {
            return async (content) =>
            {
                var response = await _message.RespondAsync(content, true);
                await DeleteMessages(response, delete, timeout);
                return response;
            };
        }

        public Func<string, DiscordEmbed, Task<DiscordMessage>> RespondEmbed(bool delete = true)
        {
            return RespondEmbed(delete, DEFAULT_TIMEOUT);
        }

        public Func<string, DiscordEmbed, Task<DiscordMessage>> RespondEmbed(TimeSpan timeout)
        {
            return RespondEmbed(DEFAULT_DELETE, timeout);
        }

        public Func<string, DiscordEmbed, Task<DiscordMessage>> RespondEmbed(bool delete, TimeSpan timeout)
        {
            return async (content, embed) =>
            {
                var response = await _message.RespondAsync(content, false, embed);
                await DeleteMessages(response, delete, timeout);
                return response;
            };
        }

        public Func<string, DiscordEmbed, Task<DiscordMessage>> RespondTTSEmbed(bool delete = true)
        {
            return RespondTTSEmbed(delete, DEFAULT_TIMEOUT);
        }

        public Func<string, DiscordEmbed, Task<DiscordMessage>> RespondTTSEmbed(TimeSpan timeout)
        {
            return RespondTTSEmbed(DEFAULT_DELETE, timeout);
        }

        public Func<string, DiscordEmbed, Task<DiscordMessage>> RespondTTSEmbed(bool delete, TimeSpan timeout)
        {
            return async (content, embed) =>
            {
                var response = await _message.RespondAsync(content, true, embed);
                await DeleteMessages(response, delete, timeout);
                return response;
            };
        }

        private async Task DeleteMessages(DiscordMessage response, bool delete, TimeSpan timeout, bool deleteResponse = true)
        {
            try
            {
                if (delete)
                {
                    try
                    {
                        await _message.DeleteAsync();
                    } catch (Exception) { }
                }
                if (timeout.TotalMilliseconds == 0 && deleteResponse)
                {
                    await response.DeleteAsync();
                }
                else if (timeout.TotalMilliseconds > 0 && deleteResponse)
                {
                    await Task.Delay(timeout).ContinueWith(async (_) =>
                    {
                        await response.DeleteAsync();
                    });
                }
            } catch (Exception ex) when (LogError(ex)) { }
        }

        private bool LogError(Exception ex)
        {
            _client.DebugLogger.LogMessage(LogLevel.Error, "B-118", "", DateTime.Now, ex);
            return false;
        }
    }
}
