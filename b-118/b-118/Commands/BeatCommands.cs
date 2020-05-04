﻿using DSharpPlus.CommandsNext;
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

namespace b_118.Commands
{
    public class BeatCommands : BaseCommandModule
    {

        LavalinkConfiguration lavalinkConfiguration { get; set; }
        ConcurrentDictionary<ulong, Queue> queues { get; set; }
        ConcurrentDictionary<ulong, bool> loops { get; set; }
        CustomPrefix _prefix;

        public BeatCommands()
        {
            lavalinkConfiguration = Program.GetLavalinkConfiguration();
            queues = new ConcurrentDictionary<ulong, Queue>();
            loops = new ConcurrentDictionary<ulong, bool>();
            _prefix = new CustomPrefix("beat");
        }

        [Command("loop")]
        [Description("B-118 will loop the current queue, or stop looping if loop was true.")]
        public async Task Loop(CommandContext ctx, [Description("Whether to loop or not")] bool? loop = null)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                bool next = loop ?? !loops[ctx.Guild.Id];
                loops[ctx.Guild.Id] = next;
                string message = next ? "Now looping" : "No longer looping.";
                await Messaging.RespondContent(ctx)(message);
            });
        }

        [Command("join")]
        [Description("B-118 will join the specified channel, or if not given, the current voice channel the user is in.")]
        public async Task Join(CommandContext ctx, [Description("The channel to add B-118 to.")] DiscordChannel channel = null)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalink = ctx.Client.GetLavalink();
                var lavalinkNodeConnection = lavalink.GetNodeConnection(Program.GetLavalinkConnectionEndpoint());
                if (lavalinkNodeConnection != null)
                    throw new InvalidOperationException("Already connected in this guild.");

                lavalinkNodeConnection = await lavalink.ConnectAsync(lavalinkConfiguration);

                if (channel == null)
                    channel = ctx.Member?.VoiceState?.Channel;

                if (channel == null)
                    throw new InvalidOperationException("You need to be in a voice channel.");

                var lavalinkGuildConnection = await lavalinkNodeConnection.ConnectAsync(channel);
                queues.TryAdd(ctx.Guild.Id, new Queue());
                loops.TryAdd(ctx.Guild.Id, false);
                lavalinkGuildConnection.PlaybackFinished += PlayNextSong(ctx, lavalinkNodeConnection, lavalinkGuildConnection);
                await Messaging.RespondContent(ctx)($"Joining {channel.Name}");
            });
        }

        [Command("play")]
        [Description("B-118 will attempt to play the given audio.")]
        public async Task Play(CommandContext ctx, [Description("URI to the audio to play.")] string uri)
        {
            await _prefix.Verify(ctx.Prefix, async () => {
                var lavalink = ctx.Client.GetLavalink();
                var lavalinkNodeConnection = lavalink.GetNodeConnection(Program.GetLavalinkConnectionEndpoint());
                if (lavalinkNodeConnection == null)
                    throw new InvalidOperationException("Not connected in this guild.");
                var track = lavalinkNodeConnection.Rest.GetTracksAsync(new Uri(uri)).GetAwaiter().GetResult().Tracks.First();
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                if (lavalinkGuildConnection.CurrentState.CurrentTrack == null)
                {
                    await lavalinkGuildConnection.PlayAsync(track);
                    if (loops[ctx.Guild.Id])
                        queues[ctx.Guild.Id].Enqueue(track);
                    await Messaging.RespondContent(ctx, true, track.Length)($"🎤 {track.Author} - {track.Title}");
                }
                else
                {
                    queues[ctx.Guild.Id].Enqueue(track);
                    await Messaging.RespondContent(ctx)("Added to the queue.");
                }
            });
        }

        [Command("stop")]
        [Description("B-118 will stop playing the current audio.")]
        public async Task Stop(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalink = ctx.Client.GetLavalink();
                var lavalinkNodeConnection = lavalink.GetNodeConnection(Program.GetLavalinkConnectionEndpoint());
                if (lavalinkNodeConnection == null)
                    throw new InvalidOperationException("Not connected in this guild.");
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                queues[ctx.Guild.Id].Clear();
                loops[ctx.Guild.Id] = false;
                await lavalinkGuildConnection.StopAsync();

                await Messaging.RespondContent(ctx)("Queue has been emptied and looping has been turned off.");
            });
        }

        [Command("pause")]
        [Description("B-118 will pause the current audio.")]
        public async Task Pause(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalink = ctx.Client.GetLavalink();
                var lavalinkNodeConnection = lavalink.GetNodeConnection(Program.GetLavalinkConnectionEndpoint());
                if (lavalinkNodeConnection == null)
                    throw new InvalidOperationException("Not connected in this guild.");
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                await lavalinkGuildConnection.PauseAsync();

                await Messaging.RespondContent(ctx)("Paused.");
            });
        }

        [Command("resume")]
        [Description("B-118 will resume the paused audio.")]
        public async Task Resume(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalink = ctx.Client.GetLavalink();
                var lavalinkNodeConnection = lavalink.GetNodeConnection(Program.GetLavalinkConnectionEndpoint());
                if (lavalinkNodeConnection == null)
                    throw new InvalidOperationException("Not connected in this guild.");
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                await lavalinkGuildConnection.ResumeAsync();

                await Messaging.RespondContent(ctx)("Resumed.");
            });
        }

        [Command("volume")]
        [Description("B-118 will set the volume of the audio.")]
        public async Task Volume(CommandContext ctx, [Description("The volume to set it to. Min. 0, Max. 100.")] int volume)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalink = ctx.Client.GetLavalink();
                var lavalinkNodeConnection = lavalink.GetNodeConnection(Program.GetLavalinkConnectionEndpoint());
                if (lavalinkNodeConnection == null)
                    throw new InvalidOperationException("Not connected in this guild.");
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                await lavalinkGuildConnection.SetVolumeAsync(volume);

                await Messaging.RespondContent(ctx)($"Set the volume to {volume}%");
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
                var lavalinkNodeConnection = lavalink.GetNodeConnection(Program.GetLavalinkConnectionEndpoint());
                if (lavalinkNodeConnection == null)
                    throw new InvalidOperationException("Not connected in this guild.");
                var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                TimeSpan currentPosition = lavalinkGuildConnection.CurrentState.PlaybackPosition;
                TimeSpan position = new TimeSpan(hours, minutes, seconds);
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

                await Messaging.RespondContent(ctx)($"Seeking to {currentPosition}.");
            });
        }

        [Command("leave")]
        [Description("B-118 will leave the voice channel.")]
        public async Task Leave(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalink = ctx.Client.GetLavalink();
                var lavalinkNodeConnection = lavalink.GetNodeConnection(Program.GetLavalinkConnectionEndpoint());
                if (lavalinkNodeConnection == null)
                    throw new InvalidOperationException("Not connected in this guild.");

                await lavalinkNodeConnection.StopAsync();
                queues = null;
                loops = null;

                await Messaging.RespondContent(ctx)("Take it sleazy!");
            });
        }

        public DSharpPlus.AsyncEventHandler<TrackFinishEventArgs> PlayNextSong(CommandContext ctx, LavalinkNodeConnection lavalinkNodeConnection, LavalinkGuildConnection lavalinkGuildConnection)
        {
            return async (TrackFinishEventArgs e) =>
            {
                if (queues[ctx.Guild.Id].Count > 0)
                {
                    var track = (LavalinkTrack) queues[ctx.Guild.Id].Dequeue();
                    await lavalinkGuildConnection.PlayAsync(track);
                    await Messaging.RespondContent(ctx, false, track.Length)($"🎤 {track.Author} - {track.Title}");
                    if (loops[ctx.Guild.Id])
                    {
                        var newTrack = lavalinkNodeConnection.Rest.GetTracksAsync(e.Track.Uri).GetAwaiter().GetResult().Tracks.First();
                        queues[ctx.Guild.Id].Enqueue(newTrack);
                    }
                }
                else
                    await Messaging.RespondContent(ctx, false)("Finished with Queue.");
            };
        }

    }
}
