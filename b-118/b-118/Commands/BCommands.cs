using b_118.Aspects;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace b_118.Commands
{
    public class BCommands : BaseCommandModule
    {
        private CustomPrefix _prefix;

        public BCommands() : base()
        {
            _prefix = new CustomPrefix("_");
        }

        [Command("beep")]
        public async Task Beep(CommandContext ctx)
        {
            await _prefix.Verify(ctx.Prefix, async () =>
            {
                await ctx.RespondAsync("boop");
            });
        }

    }
}
