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
using System.IO;

namespace b_118.Commands
{
    [Description("Commands for playing audio.")]
    class BeatCommands : BaseCommandModule
    {

        private LavalinkConfiguration _lavalinkConfiguration { get; set; }
        private ConcurrentDictionary<ulong, Queue> _queues { get; set; }
        private ConcurrentDictionary<ulong, bool> _loops { get; set; }
        public readonly CustomPrefix _prefix;

        public BeatCommands()
        {
            _lavalinkConfiguration = Program.GetLavalinkConfiguration();
            _queues = new ConcurrentDictionary<ulong, Queue>();
            _loops = new ConcurrentDictionary<ulong, bool>();
            _prefix = new CustomPrefix("beat");
        }

        private async Task<LavalinkNodeConnection> GetNodeConnection(CommandContext ctx, bool newConnection = false)
        {
            var lavalink = ctx.Client.GetLavalink();
            var lavalinkNodeConnection = lavalink.GetNodeConnection(Program.GetLavalinkConnectionEndpoint());
            if (lavalinkNodeConnection != null && newConnection)
                throw new InvalidOperationException("Already connected in this guild.");
            return await lavalink.ConnectAsync(_lavalinkConfiguration);
        }

        private async Task<LavalinkGuildConnection> GetGuildConnection(CommandContext ctx, DiscordChannel channel, LavalinkNodeConnection lavalinkNodeConnection)
        {
            if (channel == null)
                channel = ctx.Member?.VoiceState?.Channel;

            if (channel == null)
                throw new InvalidOperationException("You need to be in a voice channel.");

            return await lavalinkNodeConnection.ConnectAsync(channel);
        }

        [Command("loop")]
        [Description("B-118 will loop the current queue, or stop looping if loop was true.")]
        public async Task Loop(CommandContext ctx, [Description("Whether to loop or not")] bool? loop = null)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                bool next = loop ?? !_loops[ctx.Guild.Id];
                _loops[ctx.Guild.Id] = next;
                string message = next ? "Now looping" : "No longer looping.";
                Messaging messaging = new Messaging(ctx);
                await messaging.RespondContent()(message);
            });
        }

        [Command("enter")]
        [Description("B-118 will join the specified channel, or if not given, the current voice channel the user is in.")]
        public async Task Join(CommandContext ctx, [Description("The channel to add B-118 to.")] DiscordChannel channel = null)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalinkNodeConnection = await GetNodeConnection(ctx, true);
                var lavalinkGuildConnection = await GetGuildConnection(ctx, channel, lavalinkNodeConnection);
                _queues.TryAdd(ctx.Guild.Id, new Queue());
                _loops.TryAdd(ctx.Guild.Id, false);
                lavalinkGuildConnection.PlaybackFinished += PlayNextSong(ctx, lavalinkNodeConnection, lavalinkGuildConnection);
                Messaging messaging = new Messaging(ctx);
                await messaging.RespondContent()($"Joining {lavalinkGuildConnection.Channel.Name}");
            });
        }

        [Command("list")]
        [Description("B-118 will list the current queue.")]
        public async Task List(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                Queue q = null;
                try
                {
                    q = _queues[ctx.Guild.Id];
                } catch (System.Collections.Generic.KeyNotFoundException) {
                    throw new InvalidOperationException("There is no queue in this server.");
                }
                if (q == null)
                {
                    Messaging messaging = new Messaging(ctx);
                    await messaging.RespondContent()("There is no queue in this server.");
                } else if (q.Count > 0)
                {
                    string m = "**Queue**";
                    int count = 1;
                    foreach (LavalinkTrack i in q)
                    {
                        m += $"\n[{count++}] {i.Author} - {i.Title}";
                    }
                    Messaging messaging = new Messaging(ctx);
                    await messaging.RespondContent()(m);
                } else
                {
                    Messaging messaging = new Messaging(ctx);
                    await messaging.RespondContent()("Queue is empty.");
                }
            });
        }

        [Command("play")]
        [Description("B-118 will attempt to play the given audio.")]
        public async Task Play(CommandContext ctx, [Description("URI to the audio to play.")] string uri)
        {
            await _prefix.Verify(ctx.Prefix, async () => {
                var lavalinkNodeConnection = await GetNodeConnection(ctx);
                var track = lavalinkNodeConnection.Rest.GetTracksAsync(new Uri(uri)).GetAwaiter().GetResult().Tracks.First();
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                if (lavalinkGuildConnection.CurrentState.CurrentTrack == null)
                {
                    await lavalinkGuildConnection.PlayAsync(track);
                    if (_loops[ctx.Guild.Id])
                        _queues[ctx.Guild.Id].Enqueue(track);
                    Messaging messaging = new Messaging(ctx);
                    await messaging.RespondContent(true, track.Length)($"🎤 {track.Author} - {track.Title}");
                }
                else
                {
                    _queues[ctx.Guild.Id].Enqueue(track);
                    Messaging messaging = new Messaging(ctx);
                    await messaging.RespondContent()("Added to the queue.");
                }
            });
        }

        [Command("stop")]
        [Description("B-118 will stop playing the current audio.")]
        public async Task Stop(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalinkNodeConnection = await GetNodeConnection(ctx);
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                _queues[ctx.Guild.Id].Clear();
                _loops[ctx.Guild.Id] = false;
                await lavalinkGuildConnection.StopAsync();

                Messaging messaging = new Messaging(ctx);
                await messaging.RespondContent()("⏹️ Queue has been emptied and looping has been turned off.");
            });
        }

        [Command("pause")]
        [Description("B-118 will pause the current audio.")]
        public async Task Pause(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalinkNodeConnection = await GetNodeConnection(ctx);
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                await lavalinkGuildConnection.PauseAsync();

                Messaging messaging = new Messaging(ctx);
                await messaging.RespondContent()("⏸️ Paused.");
            });
        }

        [Command("resume")]
        [Description("B-118 will resume the paused audio.")]
        public async Task Resume(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalinkNodeConnection = await GetNodeConnection(ctx);
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                await lavalinkGuildConnection.ResumeAsync();

                Messaging messaging = new Messaging(ctx);
                await messaging.RespondContent()("▶️ Resumed.");
            });
        }

        [Command("volume")]
        [Description("B-118 will set the volume of the audio.")]
        public async Task Volume(CommandContext ctx, [Description("The volume to set it to. Min. 0, Max. 200.")] int volume)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                if (volume < 0 || volume > 200)
                {
                    throw new InvalidOperationException($"Volume `{volume}` is not between 0 and 200.");
                }
                var lavalinkNodeConnection = await GetNodeConnection(ctx);
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                await lavalinkGuildConnection.SetVolumeAsync(volume);

                Messaging messaging = new Messaging(ctx);
                string emoji = volume < 25 ? "🔈" : volume < 75 ? "🔉" : "🔊";
                await messaging.RespondContent()($"{emoji} Set the volume to {volume}%");
            });
        }

        [Command("seek")]
        [Description("B-118 will seek to the given time.")]
        public async Task Seek(CommandContext ctx, [Description("Time to seek to.")] string time, [Description("Set to `+` or `-` to seek a relative time.")] string action = "seek")
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                int hours, minutes, seconds;
                var times = time.Split(":");
                if (times.Length >= 3)
                {
                    hours = int.Parse(times[0]);
                    minutes = int.Parse(times[1]);
                    seconds = int.Parse(times[2]);
                }
                else if (times.Length == 2)
                {
                    hours = 0;
                    minutes = int.Parse(times[0]);
                    seconds = int.Parse(times[1]);
                }
                else
                {
                    hours = 0;
                    minutes = 0;
                    seconds = int.Parse(times[0]);
                }
                var lavalink = ctx.Client.GetLavalink();
                var lavalinkNodeConnection = await GetNodeConnection(ctx);
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                TimeSpan currentPosition = lavalinkGuildConnection.CurrentState.PlaybackPosition;
                TimeSpan position = new TimeSpan(hours, minutes, seconds);
                string emoji = action == "+" || action == "add" ? "⏩" : action == "subtract" || action == "-" ? "⏪" : currentPosition > position ? "⏪" : "⏩";
                if (action == "+" || action == "add")
                {
                    currentPosition += position;
                }
                else if (action == "-" || action == "subtract")
                {
                    currentPosition -= position;
                }
                else
                    currentPosition = position;
                await lavalinkGuildConnection.SeekAsync(currentPosition);

                Messaging messaging = new Messaging(ctx);
                await messaging.RespondContent()($"{emoji} Seeking to {currentPosition}.");
            });
        }

        [Command("exit")]
        [Description("B-118 will leave the voice channel.")]
        public async Task Leave(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalinkNodeConnection = await GetNodeConnection(ctx);

                await lavalinkNodeConnection.StopAsync();
                _queues = null;
                _loops = null;

                Messaging messaging = new Messaging(ctx);
                await messaging.RespondContent()("Take it sleazy!");
            });
        }

        public DSharpPlus.AsyncEventHandler<TrackFinishEventArgs> PlayNextSong(CommandContext ctx, LavalinkNodeConnection lavalinkNodeConnection, LavalinkGuildConnection lavalinkGuildConnection)
        {
            return async (TrackFinishEventArgs e) =>
            {
                if (_queues[ctx.Guild.Id].Count > 0)
                {
                    var track = (LavalinkTrack) _queues[ctx.Guild.Id].Dequeue();
                    await lavalinkGuildConnection.PlayAsync(track);
                    Messaging messaging = new Messaging(ctx);
                    await messaging.RespondContent(false, track.Length)($"🎤 {track.Author} - {track.Title}");
                    if (_loops[ctx.Guild.Id])
                    {
                        var newTrack = lavalinkNodeConnection.Rest.GetTracksAsync(e.Track.Uri).GetAwaiter().GetResult().Tracks.First();
                        _queues[ctx.Guild.Id].Enqueue(newTrack);
                    }
                }
            };
        }

    }
}
