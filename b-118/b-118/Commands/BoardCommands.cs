using b_118.Aspects;
using b_118.Utility;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Lavalink;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace b_118.Commands
{
  [Description("Commands for playing quick sound effects.")]
  class BoardCommands : BaseCommandModule
  {

    private ConcurrentDictionary<ulong, Queue> queues { get; set; }
    private ConcurrentDictionary<ulong, bool> loops { get; set; }
    public readonly CustomPrefix _prefix;

    public BoardCommands()
    {
      queues = new ConcurrentDictionary<ulong, Queue>();
      loops = new ConcurrentDictionary<ulong, bool>();
      _prefix = new CustomPrefix("b-sound");
    }

    [Command("board")]
    [RequirePrefixes("b-sound")]
    [Description("B-118 will play a sound from his sound board.")]
    public async Task Board(CommandContext ctx, [Description("The sound to play.")] string sound, [Description("The volume to play the sound at.")] int? volume = null, [Description("The volume to set after the sound plays.")] int? postVolume = null)
    {
      LavalinkNodeConnection lavalinkNodeConnection = await Connections.GetNodeConnection(ctx);
      FileInfo clip = Program.GetB118SoundClip().LoadClip(sound);
      if (Program.GetB118SoundClip().Verify(clip))
      {
        LavalinkTrack track = lavalinkNodeConnection.Rest.GetTracksAsync(clip).GetAwaiter().GetResult().Tracks.First();
        LavalinkGuildConnection lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
        if (volume.HasValue && volume >= 0 && volume <= 200)
        {
          await lavalinkGuildConnection.SetVolumeAsync(volume.Value);
        }
        if (lavalinkGuildConnection.CurrentState.CurrentTrack == null)
        {
          await lavalinkGuildConnection.PlayAsync(track);
          TimeSpan length = track.Length;
          await Task.Delay(length).ContinueWith(async (_) =>
          {
            if (postVolume.HasValue && postVolume >= 0 && postVolume <= 200)
            {
              await lavalinkGuildConnection.SetVolumeAsync(postVolume.Value);
            }
          });
          await ctx.Message.DeleteAsync();
        }
        else
        {
          LavalinkTrack previousTrack = lavalinkGuildConnection.CurrentState.CurrentTrack;
          TimeSpan position = lavalinkGuildConnection.CurrentState.PlaybackPosition;
          await lavalinkGuildConnection.PlayAsync(track);
          TimeSpan length = track.Length;
          await Task.Delay(length).ContinueWith(async (_) =>
          {
            if (postVolume.HasValue && postVolume >= 0 && postVolume <= 200)
            {
              await lavalinkGuildConnection.SetVolumeAsync(postVolume.Value);
            }
            await lavalinkGuildConnection.PlayPartialAsync(previousTrack, position, previousTrack.Length);
          });
          await ctx.Message.DeleteAsync();
        }
      }
    }

    [Command("boards")]
    [RequirePrefixes("b-sound")]
    [Hidden]
    public async Task Boards(CommandContext ctx)
    {
      string m = "**Boards**";
      foreach (string name in Program.GetB118SoundClip().ListDirectories())
      {
        m += $"\n{name}";
      }
      Messaging messaging = new Messaging(ctx, true);
      await messaging.RespondContent()(m);
    }

    [Command("boards")]
    [RequirePrefixes("b-sound")]
    [Description("List the available sound boards, or the available sounds in a given board.")]
    public async Task Boards(CommandContext ctx, [Description("The board to list the sounds of.")] string board)
    {
      string m = $"**{board} Sounds**";
      foreach (string name in Program.GetB118SoundClip().ListFileNames(board))
      {
        m += $"\n{board}/{name}";
      }
      Messaging messaging = new Messaging(ctx, true);
      await messaging.RespondContent()(m);
    }
  }
}
