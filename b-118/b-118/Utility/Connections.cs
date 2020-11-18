using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;

namespace b_118.Utility {
  class Connections {

    public static async Task<LavalinkNodeConnection> GetNodeConnection(CommandContext ctx, bool newConnection = false)
    {
      LavalinkExtension lavalink = ctx.Client.GetLavalink();
      LavalinkNodeConnection lavalinkNodeConnection = lavalink.GetNodeConnection(Program.GetLavalinkConnectionEndpoint());
      if (lavalinkNodeConnection == null)
      {
        lavalinkNodeConnection = await lavalink.ConnectAsync(Program.GetLavalinkConfiguration());
      }
      return lavalinkNodeConnection;
    }

    public static async Task<LavalinkGuildConnection> GetGuildConnection(CommandContext ctx, DiscordChannel channel, LavalinkNodeConnection lavalinkNodeConnection)
    {
      if (channel == null)
        channel = ctx.Member?.VoiceState?.Channel;

      if (channel == null)
        throw new InvalidOperationException("You need to be in a voice channel.");

      if (lavalinkNodeConnection.GetGuildConnection(ctx.Guild) != null)
      {
        throw new InvalidOperationException("I am already connected in this guild.");
      }

      return await lavalinkNodeConnection.ConnectAsync(channel);
    }

  }
}
