using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace b_118.Commands
{
    public class BCommands : BaseCommandModule
    {
        [Command("beep")]
        public async Task Beep(CommandContext ctx)
        {
            await ctx.RespondAsync("boop");
        }

    }
}
