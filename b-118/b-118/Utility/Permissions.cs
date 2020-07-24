using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace b_118.Utility
{
    /// <summary>
    /// Utility Methods for checking Bot Permissions.
    /// </summary>
    public class Permissions
    {
        private readonly DiscordGuild _guild;
        private readonly DiscordUser _user;

        /// <summary>
        /// Creates a new instance of the <see cref="Permissions"/> class using a DiscordGuild.
        /// </summary>
        /// <param name="guild">The Guild for retrieving the permissions.</param>
        public Permissions(DiscordGuild guild, DiscordUser user)
        {
            _guild = guild;
            _user = user;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Permissions"/> class using a CommandContext.
        /// </summary>
        /// <param name="context">The CommandContext for retrieving the permissions.</param>
        public Permissions(CommandContext context)
        {
            _guild = context.Guild;
            _user = context.Client.CurrentUser;
        }

        private DSharpPlus.Permissions GetPermissions()
        {
            var name = _user.Username;
            return _guild.Roles.First(role => role.Value.Name.Equals(name)).Value.Permissions;
        }

        public bool HasPermission(DSharpPlus.Permissions permission)
        {
            return GetPermissions().HasPermission(permission);
        }

        public bool HasPermissions(params DSharpPlus.Permissions[] permissions)
        {
            if (permissions.Length == 0)
                throw new ArgumentException("Permissions cannot be empty.");
            foreach (DSharpPlus.Permissions permission in permissions)
            {
                if (!HasPermission(permission))
                {
                    return false;
                }
            }
            return true;
        }

        public bool CanManageCampaign()
        {
            return HasPermissions(DSharpPlus.Permissions.ManageRoles, DSharpPlus.Permissions.ManageChannels);
        }


    }
}
