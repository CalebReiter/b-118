using DSharpPlus.CommandsNext;
using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus;
using Microsoft.Extensions.Logging;

namespace b_118.Utility
{
  class Messaging
  {
    static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(10);
    static readonly bool DEFAULT_DELETE = true;
    private readonly CommandContext _context;
    private readonly DiscordClient _client;
    private readonly DiscordMessage _message;
    private readonly bool _directMessage;

    public Messaging(CommandContext context)
    {
        _directMessage = false;
      _context = context;
      _client = context.Client;
      _message = context.Message;
    }
    public Messaging(CommandContext context, bool directMessage)
    {
      _directMessage = directMessage;
      _context = context;
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
          DiscordMessage response;
          if (_directMessage) {
            DiscordDmChannel dm = await _context.Member.CreateDmChannelAsync();
            response = await dm.SendMessageAsync(content);
          } else {
            response = await _message.RespondAsync(content);
            await DeleteMessages(response, delete, timeout, deleteResponse);
          }
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
        DiscordMessage response;
        if (_directMessage)
        {
          DiscordDmChannel dm = await _context.Member.CreateDmChannelAsync();
          response = await dm.SendMessageAsync(content, true);
        }
        else
        {
            response = await _message.RespondAsync(content, true);
            await DeleteMessages(response, delete, timeout);
        }
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
        DiscordMessage response;
        if (_directMessage)
        {
            DiscordDmChannel dm = await _context.Member.CreateDmChannelAsync();
            response = await dm.SendMessageAsync(content, false, embed);
        }
        else
        {
            response = await _message.RespondAsync(content, false, embed);
            await DeleteMessages(response, delete, timeout);
        }
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
        DiscordMessage response;
        if (_directMessage)
        {
          DiscordDmChannel dm = await _context.Member.CreateDmChannelAsync();
          response = await dm.SendMessageAsync(content, true, embed);
        }
        else
        {
          response = await _message.RespondAsync(content, true, embed);
          await DeleteMessages(response, delete, timeout);
        }
        return response;
      };
    }

    private async Task DeleteMessages(DiscordMessage response, bool delete, TimeSpan timeout, bool deleteResponse = true)
    {
      try
      {
        if (delete && !_context.Channel.IsPrivate)
        {
          try
          {
            await _message.DeleteAsync();
          }
          catch (Exception) { }
        }
        if (timeout.TotalMilliseconds == 0 && deleteResponse && !_context.Channel.IsPrivate)
        {
          await response.DeleteAsync();
        }
        else if (timeout.TotalMilliseconds > 0 && deleteResponse && !_context.Channel.IsPrivate)
        {
          await Task.Delay(timeout).ContinueWith(async (_) =>
          {
            await response.DeleteAsync();
          });
        }
      }
      catch (Exception ex) when (LogError(ex)) { }
    }

    private bool LogError(Exception ex)
    {
      _client.Logger.Log(LogLevel.Error, "B-118", "", DateTime.Now, ex);
      return false;
    }
  }
}
