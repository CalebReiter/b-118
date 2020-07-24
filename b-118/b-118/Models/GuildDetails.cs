using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace b_118.Models
{
    class GuildDetails
    {
        public static ConcurrentDictionary<ulong, GuildDetails> ClientGuilds = new ConcurrentDictionary<ulong, GuildDetails>();
        ConcurrentDictionary<string, bool> _cooldowns;
        private bool loopQueue { get; set; }
        private ConcurrentQueue<LavalinkTrack> queue { get; set; }

        public GuildDetails()
        {
            _cooldowns = new ConcurrentDictionary<string, bool>();
            string [] cooldownKeys = new string[] { "beep", "bees?", "beereaction" };
            foreach (string key in cooldownKeys)
            {
                _cooldowns.TryAdd(key, false);
            }
            loopQueue = false;
            queue = new ConcurrentQueue<LavalinkTrack>();
        }

        public static void AddClientGuild(DiscordClient discord, DiscordGuild guild)
        {
            discord.DebugLogger.LogMessage(LogLevel.Info, "B-118", $"{guild.Name} has joined the fleet!", DateTime.Now);
            ClientGuilds[guild.Id] = new GuildDetails();
        }

        public void SetCooldown(string key, TimeSpan timespan)
        {
            _cooldowns[key] = true;
            Task.Delay(timespan).ContinueWith((_) =>
            {
                _cooldowns[key] = false;
            });
        }

        public bool GetCooldown(string key)
        {
            return _cooldowns[key];
        }
    }
}
