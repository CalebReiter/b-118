using b_118.Aspects;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace b_118.Commands
{
    [Description("Commands for playing quick sound effects.")]
    class BoardCommands : BaseCommandModule
    {

        private LavalinkConfiguration _lavalinkConfiguration { get; set; }
        private ConcurrentDictionary<ulong, Queue> _queues { get; set; }
        private ConcurrentDictionary<ulong, bool> _loops { get; set; }
        public readonly CustomPrefix _prefix;

        public BoardCommands()
        {
            _lavalinkConfiguration = Program.GetLavalinkConfiguration();
            _queues = new ConcurrentDictionary<ulong, Queue>();
            _loops = new ConcurrentDictionary<ulong, bool>();
            _prefix = new CustomPrefix("sound");
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

        [Command("board")]
        [Description("B-118 will play a sound from his sound board.")]
        public async Task Board(CommandContext ctx, [Description("The sound to play.")] string sound, [Description("The volume to play the sound at.")] int? volume = null, [Description("The volume to set after the sound plays.")] int? postVolume = null)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                var lavalinkNodeConnection = await GetNodeConnection(ctx);
                FileInfo clip = Program.GetB118SoundClip().LoadClip(sound);
                if (Program.GetB118SoundClip().Verify(clip))
                {
                    var track = lavalinkNodeConnection.Rest.GetTracksAsync(clip).GetAwaiter().GetResult().Tracks.First();
                    var lavalinkGuildConnection = lavalinkNodeConnection.GetConnection(ctx.Guild);
                    if (volume.HasValue && volume >= 0 && volume <= 200)
                    {
                        await lavalinkGuildConnection.SetVolumeAsync(volume.Value);
                    }
                    if (lavalinkGuildConnection.CurrentState.CurrentTrack == null)
                    {
                        await lavalinkGuildConnection.PlayAsync(track);
                        var length = track.Length;
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
                        var previousTrack = lavalinkGuildConnection.CurrentState.CurrentTrack;
                        var position = lavalinkGuildConnection.CurrentState.PlaybackPosition;
                        await lavalinkGuildConnection.PlayAsync(track);
                        var length = track.Length;
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
            });
        }

        [Command("boards")]
        [Description("List the available sound boards.")]
        public async Task Boards(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                string m = "**Boards**";
                foreach (var name in Program.GetB118SoundClip().ListDirectories())
                {
                    m += $"\n{name}";
                }
                if (ctx.Channel.IsPrivate)
                {
                    await ctx.Message.RespondAsync(m);
                }
                else
                {
                    DiscordDmChannel dm = await ctx.Member.CreateDmChannelAsync();
                    await dm.SendMessageAsync(m);
                    await ctx.Message.DeleteAsync();
                }
            });
        }

        [Command("board-sounds")]
        [Description("List the available sounds in a given board.")]
        public async Task Boards(CommandContext ctx, [Description("The board to list the sounds of.")] string board)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                string m = $"**{board} Sounds**";   
                foreach (var name in Program.GetB118SoundClip().ListFileNames(board))
                {
                    m += $"\n{board}/{name}";
                }
                if (ctx.Channel.IsPrivate)
                {
                    await ctx.Message.RespondAsync(m);
                } else
                {
                    DiscordDmChannel dm = await ctx.Member.CreateDmChannelAsync();
                    await dm.SendMessageAsync(m);
                    await ctx.Message.DeleteAsync();
                }
            });
        }
    }
}
