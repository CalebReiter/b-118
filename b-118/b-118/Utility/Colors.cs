using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace b_118.Utility
{
    class Colors
    {

        public readonly Random random = new Random();
        public readonly DiscordColor Red;
        public readonly DiscordColor RedOrange;
        public readonly DiscordColor Orange;
        public readonly DiscordColor YellowOrange;
        public readonly DiscordColor Yellow;
        public readonly DiscordColor YellowGreen;
        public readonly DiscordColor Green;
        public readonly DiscordColor BlueGreen;
        public readonly DiscordColor Blue;
        public readonly DiscordColor BlueViolet;
        public readonly DiscordColor Violet;
        public readonly DiscordColor RedViolet;
        public readonly DiscordColor White;
        public readonly DiscordColor Gray;
        public readonly DiscordColor Black;
        public readonly DiscordColor[] colors;

        private static DiscordColor GetColor(IConfiguration configuration, string key, string fallback)
        {
            var section = configuration.GetSection("Colors");
            if (section.Exists())
            {
                if (section[key] != null)
                {
                    return new DiscordColor(section[key]);
                }
            }
            return new DiscordColor(fallback);
        }

        public DiscordColor GetRandomColor()
        {
            return colors[random.Next(colors.Length)];
        }

        public Colors(IConfiguration configuration)
        {
            Red = GetColor(configuration, "Red","#ff2929");
            RedOrange = GetColor(configuration, "RedOrange", "#f56042");
            Orange = GetColor(configuration, "Orange", "#ef7d43");
            YellowOrange = GetColor(configuration, "YellowOrange", "#f9af3a");
            Yellow = GetColor(configuration, "Yellow", "#f0e13c");
            YellowGreen = GetColor(configuration, "YellowGreen", "#c5e259");
            Green = GetColor(configuration, "Green", "#9ad74c");
            BlueGreen = GetColor(configuration, "BlueGreen", "#82cdb9");
            Blue = GetColor(configuration, "Blue", "#3fb8cd");
            BlueViolet = GetColor(configuration, "BlueViolet", "#4b30d2");
            Violet = GetColor(configuration, "Violet", "#780a49");
            RedViolet = GetColor(configuration, "RedViolet", "#d2304b");
            White = GetColor(configuration, "White", "#fdfbdf");
            Gray = GetColor(configuration, "Gray", "#36393f");
            Black = GetColor(configuration, "Black", "#200213");
            colors = new DiscordColor[]
            {
                Red,
                RedOrange,
                Orange,
                YellowOrange,
                Yellow,
                YellowGreen,
                Green,
                BlueGreen,
                Blue,
                BlueViolet,
                Violet,
                RedViolet,
                White,
                Gray,
                Black
            };
        }

    }
}
