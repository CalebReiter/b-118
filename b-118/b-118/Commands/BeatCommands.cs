using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections;
using DSharpPlus.Lavalink.EventArgs;
using b_118.Utility;
using b_118.Aspects;
using DSharpPlus.Lavalink.Entities;
using System.Collections.Generic;

namespace b_118.Commands
{
  [Description("Commands for playing audio.")]
  class BeatCommands : BaseCommandModule
  {

    private ConcurrentDictionary<ulong, Queue> queues { get; set; }
    private ConcurrentDictionary<ulong, bool> loops { get; set; }
    public readonly CustomPrefix _prefix;

    public BeatCommands()
    {
      queues = new ConcurrentDictionary<ulong, Queue>();
      loops = new ConcurrentDictionary<ulong, bool>();
      _prefix = new CustomPrefix("b-beat");
    }

    [Command("loop")]
    [RequirePrefixes("b-beat")]
    [Description("B-118 will loop the current queue, or stop looping if loop was true.")]
    public async Task Loop(CommandContext ctx, [Description("Whether to loop or not")] bool? loop = null)
    {
      bool next = loop ?? !loops[ctx.Guild.Id];
      loops[ctx.Guild.Id] = next;
      string message = next ? "Now looping" : "No longer looping.";
      Messaging messaging = new Messaging(ctx);
      await messaging.RespondContent()(message);
    }

    [Command("enter")]
    [RequirePrefixes("b-beat")]
    [Description("B-118 will join the specified channel, or if not given, the current voice channel the user is in.")]
    public async Task Join(CommandContext ctx, [Description("The channel to add B-118 to.")] DiscordChannel channel = null)
    {
      LavalinkNodeConnection lavalinkNodeConnection = await Connections.GetNodeConnection(ctx, true);
      LavalinkGuildConnection lavalinkGuildConnection = await Connections.GetGuildConnection(ctx, channel, lavalinkNodeConnection);
      queues.TryAdd(ctx.Guild.Id, new Queue());
      loops.TryAdd(ctx.Guild.Id, false);
      await lavalinkGuildConnection.SetVolumeAsync(25);
      lavalinkGuildConnection.PlaybackFinished += PlayNextSong(ctx, lavalinkNodeConnection, lavalinkGuildConnection);
      Messaging messaging = new Messaging(ctx);
      await messaging.RespondContent()($"Joining {lavalinkGuildConnection.Channel.Name}");
    }

    [Command("list")]
    [RequirePrefixes("b-beat")]
    [Description("B-118 will list the current queue.")]
    public async Task List(CommandContext ctx)
    {
      Queue queue = null;
      try
      {
        queue = queues[ctx.Guild.Id];
      }
      catch (KeyNotFoundException)
      {
        throw new InvalidOperationException("There is no queue in this server.");
      }
      if (queue.Count > 0)
      {
        string m = "**Queue**";
        int count = 1;
        foreach (LavalinkTrack track in queue)
        {
          m += $"\n[{count++}] {track.Author} - {track.Title}";
        }
        Messaging messaging = new Messaging(ctx);
        await messaging.RespondContent()(m);
      }
      else
      {
        Messaging messaging = new Messaging(ctx);
        await messaging.RespondContent(TimeSpan.FromSeconds(30))("Queue is empty.");
      }
    }

    [Command("play")]
    [RequirePrefixes("b-beat")]
    [Description("B-118 will attempt to play the given audio.")]
    public async Task Play(CommandContext ctx, [Description("URI to the audio to play.")] string song, [Description("Source to search from. {Youtube, SoundCloud}")] string source = "Youtube")
    {
      LavalinkNodeConnection lavalinkNodeConnection = await Connections.GetNodeConnection(ctx);
      LavalinkTrack track = null;
      if (Uri.TryCreate(song, UriKind.Absolute, out Uri uri))
      {
        LavalinkLoadResult results = await lavalinkNodeConnection.Rest.GetTracksAsync(uri);
        if (results.Exception.Message != null) throw new Exception(results.Exception.Message);
        IEnumerable<LavalinkTrack> tracks = results.Tracks;
        if (tracks.Count() == 0) throw new InvalidOperationException($"Could not find `{uri.OriginalString}`.");
        track = tracks.First();
      }
      else
      {
        if (Enum.TryParse(source, true, out LavalinkSearchType lavalinkSearchType))
        {
          IEnumerable<LavalinkTrack> tracks = lavalinkNodeConnection.Rest.GetTracksAsync(song, lavalinkSearchType).GetAwaiter().GetResult().Tracks;
          if (tracks.Count() == 0) throw new InvalidOperationException($"There were 0 results for `{song}`.");
          track = tracks.First();
        }
        else
        {
          throw new InvalidOperationException($"`{source}` is not a valid source.");
        }
      }
      LavalinkGuildConnection lavalinkGuildConnection = lavalinkNodeConnection.GetGuildConnection(ctx.Guild);
      Messaging messaging = new Messaging(ctx);
      if (lavalinkGuildConnection.CurrentState.CurrentTrack == null)
      {
        await lavalinkGuildConnection.PlayAsync(track);
        if (loops[ctx.Guild.Id]) queues[ctx.Guild.Id].Enqueue(track);
        await messaging.RespondContent(true, track.Length)($"🎤 {track.Author} - {track.Title}");
      }
      else
      {
        queues[ctx.Guild.Id].Enqueue(track);
        await messaging.RespondContent()("Added to the queue.");
      }
    }

    [Command("skip")]
    [RequirePrefixes("b-beat")]
    [Description("B-118 will skip the rest of the current track.")]
    public async Task Skip(CommandContext ctx)
    {
      LavalinkNodeConnection lavalinkNodeConnection = await Connections.GetNodeConnection(ctx);
      LavalinkGuildConnection lavalinkGuildConnection = lavalinkNodeConnection.GetGuildConnection(ctx.Guild);
      LavalinkPlayerState currentState = lavalinkGuildConnection.CurrentState;
      if (currentState != null) await SkipSong(ctx, lavalinkNodeConnection, lavalinkGuildConnection);
    }

    [Command("stop")]
    [RequirePrefixes("b-beat")]
    [Description("B-118 will stop playing the current audio.")]
    public async Task Stop(CommandContext ctx)
    {
      LavalinkNodeConnection lavalinkNodeConnection = await Connections.GetNodeConnection(ctx);
      LavalinkGuildConnection lavalinkGuildConnection = lavalinkNodeConnection.GetGuildConnection(ctx.Guild);
      queues[ctx.Guild.Id].Clear();
      loops[ctx.Guild.Id] = false;
      await lavalinkGuildConnection.StopAsync();

      Messaging messaging = new Messaging(ctx);
      await messaging.RespondContent()("⏹️ Queue has been emptied and looping has been turned off.");
    }

    [Command("pause")]
    [RequirePrefixes("b-beat")]
    [Description("B-118 will pause the current audio.")]
    public async Task Pause(CommandContext ctx)
    {
      LavalinkNodeConnection lavalinkNodeConnection = await Connections.GetNodeConnection(ctx);
      LavalinkGuildConnection lavalinkGuildConnection = lavalinkNodeConnection.GetGuildConnection(ctx.Guild);
      await lavalinkGuildConnection.PauseAsync();

      Messaging messaging = new Messaging(ctx);
      await messaging.RespondContent()("⏸️ Paused.");
    }

    [Command("resume")]
    [RequirePrefixes("b-beat")]
    [Description("B-118 will resume the paused audio.")]
    public async Task Resume(CommandContext ctx)
    {
      LavalinkNodeConnection lavalinkNodeConnection = await Connections.GetNodeConnection(ctx);
      LavalinkGuildConnection lavalinkGuildConnection = lavalinkNodeConnection.GetGuildConnection(ctx.Guild);
      await lavalinkGuildConnection.ResumeAsync();

      Messaging messaging = new Messaging(ctx);
      await messaging.RespondContent()("▶️ Resumed.");
    }

    [Command("volume")]
    [RequirePrefixes("b-beat")]
    [Description("B-118 will set the volume of the audio.")]
    public async Task Volume(CommandContext ctx, [Description("The volume to set it to. Min. 0, Max. 200.")] int volume)
    {
      if (volume < 0 || volume > 200)
      {
        throw new InvalidOperationException($"Volume `{volume}` is not between 0 and 200.");
      }
      LavalinkNodeConnection lavalinkNodeConnection = await Connections.GetNodeConnection(ctx);
      LavalinkGuildConnection lavalinkGuildConnection = lavalinkNodeConnection.GetGuildConnection(ctx.Guild);
      await lavalinkGuildConnection.SetVolumeAsync(volume);

      Messaging messaging = new Messaging(ctx);
      string emoji = volume < 25 ? "🔈" : volume < 75 ? "🔉" : "🔊";
      await messaging.RespondContent()($"{emoji} Set the volume to {volume}%");
    }

    [Command("seek")]
    [RequirePrefixes("b-beat")]
    [Description("B-118 will seek to the given time.")]
    public async Task Seek(CommandContext ctx, [Description("Time to seek to.")] string time, [Description("Set to `+` or `-` to seek a relative time.")] string action = "seek")
    {
      Messaging messaging = new Messaging(ctx);
      TimeSpan position = new TimeSpan();
      string[] formats = new string[] { "s", "ss", "m\\:ss", "mm\\:ss", "h\\:mm\\:ss", "hh\\:mm\\:ss", null };
      string paddedStart = time.Split(":")[0].Length < 2 ? $"0{time}" : time;
      for (int i = 0; i < formats.Length; i++)
      {
        string format = formats[i];
        if (format == null) throw new InvalidOperationException($"`{time}` is in an invalid format.");
        if (TimeSpan.TryParseExact(paddedStart, format, null, out position))
        {
          break;
        }
      }
      LavalinkNodeConnection lavalinkNodeConnection = await Connections.GetNodeConnection(ctx);
      LavalinkGuildConnection lavalinkGuildConnection = lavalinkNodeConnection.GetGuildConnection(ctx.Guild);
      TimeSpan currentPosition = lavalinkGuildConnection.CurrentState.PlaybackPosition;
      string emoji = action == "+" || action == "add" ? "⏩" : action == "subtract" || action == "-" ? "⏪" : currentPosition > position ? "⏪" : "⏩";
      if (action == "+" || action == "add") currentPosition += position;
      else if (action == "-" || action == "subtract") currentPosition -= position;
      else currentPosition = position;
      await lavalinkGuildConnection.SeekAsync(currentPosition);
      await messaging.RespondContent()($"{emoji} Seeking to {currentPosition}.");
    }

    [Command("exit")]
    [RequirePrefixes("b-beat")]
    [Description("B-118 will leave the voice channel.")]
    public async Task Leave(CommandContext ctx)
    {
      LavalinkNodeConnection lavalinkNodeConnection = await Connections.GetNodeConnection(ctx);
      LavalinkGuildConnection lavalinkGuildConnection = lavalinkNodeConnection.GetGuildConnection(ctx.Guild);

      if (lavalinkGuildConnection != null)
      {
        await lavalinkGuildConnection.DisconnectAsync();
        queues[ctx.Guild.Id].Clear();
        loops[ctx.Guild.Id] = false;

        Messaging messaging = new Messaging(ctx);
        await messaging.RespondContent()("Take it sleazy!");
      }
    }

    public async Task SkipSong(CommandContext ctx, LavalinkNodeConnection lavalinkNodeConnection, LavalinkGuildConnection lavalinkGuildConnection)
    {
      await lavalinkGuildConnection.StopAsync();
      await PlayNextSongIfExists(ctx, lavalinkNodeConnection, lavalinkGuildConnection, null);
    }

    public async Task PlayNextSongIfExists(CommandContext ctx, LavalinkNodeConnection lavalinkNodeConnection, LavalinkGuildConnection lavalinkGuildConnection, Uri uri)
    {
      Messaging messaging = new Messaging(ctx);
      if (queues[ctx.Guild.Id].Count > 0)
      {
        LavalinkTrack track = (LavalinkTrack)queues[ctx.Guild.Id].Dequeue();
        await lavalinkGuildConnection.PlayAsync(track);
        await messaging.RespondContent(false, track.Length)($"🎤 {track.Author} - {track.Title}");
        if (loops[ctx.Guild.Id] && uri != null)
        {
          LavalinkTrack newTrack = lavalinkNodeConnection.Rest.GetTracksAsync(uri).GetAwaiter().GetResult().Tracks.First();
          queues[ctx.Guild.Id].Enqueue(newTrack);
        }
      }
      else
      {
        await messaging.RespondContent()("Queue has finished.");
      }
    }

    public Emzi0767.Utilities.AsyncEventHandler<LavalinkGuildConnection, TrackFinishEventArgs> PlayNextSong(CommandContext ctx, LavalinkNodeConnection lavalinkNodeConnection, LavalinkGuildConnection lavalinkGuildConnection)
    {
      return async (LavalinkGuildConnection g, TrackFinishEventArgs e) =>
      {
        await PlayNextSongIfExists(ctx, lavalinkNodeConnection, lavalinkGuildConnection, e.Track.Uri);
      };
    }

  }
}
