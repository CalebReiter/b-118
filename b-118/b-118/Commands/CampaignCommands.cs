using b_118.Aspects;
using b_118.Utility;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace b_118.Commands
{
  [Description("Commands for managing campaigns.")]
  class CampaignCommands : BaseCommandModule
  {
    public readonly CustomPrefix _prefix;

    public CampaignCommands() : base()
    {
      _prefix = new CustomPrefix("campaign");
    }

    private void CheckPermissions(CommandContext ctx)
    {
      Permissions permissions = new Permissions(ctx);
      if (!permissions.CanManageCampaign())
        throw new InvalidOperationException("I don't have the permissions to do that.");
    }

    private void CheckRole(CommandContext ctx, string roleName)
    {
      IEnumerable<DiscordRole> roles = ctx.Member.Roles;
      if (!roles.Any(role => role.Name.Equals(roleName)))
        throw new InvalidOperationException($"You must have the {roleName} role to use this command.");
    }

    private DiscordChannel GetCampaignCategory(CommandContext ctx, string name)
    {
      return ctx.Guild.Channels
          .FirstOrDefault(channel => (channel.Value.Name == name) && (channel.Value.Type == DSharpPlus.ChannelType.Category))
          .Value;
    }

    private async Task<DiscordMember> GetOwner(CommandContext ctx, DiscordChannel channel)
    {
      if (channel == null)
        return null;
      try
      {
        return await channel.Children.First(channel => channel.Name == "announcements").PermissionOverwrites.First(overwrite =>
        {
          try
          {
            overwrite.GetMemberAsync().ConfigureAwait(true).GetAwaiter().GetResult();
            return true;
          }
          catch (ArgumentException)
          {
            return false;
          }
        }).GetMemberAsync();
      }
      catch (NullReferenceException)
      {
        return null;
      }
    }

    private bool CheckOwnership(CommandContext ctx, DiscordChannel channel)
    {
      if (channel == null)
      {
        return false;
      }
      return channel.Children.First(channel => channel.Name == "announcements").PermissionOverwrites.Any((overwrite) =>
      {
        DiscordMember member;
        try
        {
          member = overwrite.GetMemberAsync().ConfigureAwait(true).GetAwaiter().GetResult();
          return member.Id.Equals(ctx.Member.Id);
        }
        catch (ArgumentException)
        {
          return false;
        }
      });
    }

    private IEnumerable<DiscordOverwriteBuilder> GetCategoryOverwriteBuilders(DiscordRole everyone, DiscordRole role)
    {
      return new DiscordOverwriteBuilder[] {
                new DiscordOverwriteBuilder()
                    .Deny(DSharpPlus.Permissions.AccessChannels)
                    .For(everyone),
                new DiscordOverwriteBuilder()
                    .Allow(DSharpPlus.Permissions.AccessChannels)
                    .For(role)
            };
    }

    private IEnumerable<DiscordOverwriteBuilder> GetAnnouncementsOverwriteBuilders(DiscordRole everyone, DiscordMember dm, DiscordRole role)
    {
      return new DiscordOverwriteBuilder[]
      {
                new DiscordOverwriteBuilder()
                    .Deny(DSharpPlus.Permissions.AccessChannels)
                    .For(everyone),
                new DiscordOverwriteBuilder()
                    .Allow(DSharpPlus.Permissions.AccessChannels)
                    .For(role),
                new DiscordOverwriteBuilder()
                    .Deny(DSharpPlus.Permissions.SendMessages)
                    .For(role),
                new DiscordOverwriteBuilder()
                    .Allow(DSharpPlus.Permissions.SendMessages)
                    .For(dm)
      };
    }

    private IEnumerable<DiscordOverwriteBuilder> GetDMNotesOverwriteBuilders(DiscordRole everyone, DiscordMember dm, DiscordRole role)
    {
      return new DiscordOverwriteBuilder[]
      {
                new DiscordOverwriteBuilder()
                    .Deny(DSharpPlus.Permissions.AccessChannels)
                    .For(everyone),
                new DiscordOverwriteBuilder()
                    .Deny(DSharpPlus.Permissions.AccessChannels)
                    .For(role),
                new DiscordOverwriteBuilder()
                    .Allow(DSharpPlus.Permissions.AccessChannels)
                    .For(dm)
      };

    }

    [Command("create")]
    [RequirePrefixes("campaign")]
    [RequireRoles(RoleCheckMode.All, "DM")]
    [Description("Create a new campaign. Requires the DM role.")]
    public async Task CreateCampaign(CommandContext ctx, [Description("Name of the campaign.")] string name, [Description("Color used for the role.")] DiscordColor? color = null, [Description("Channels to exclude from the category.")] params string[] excludeChannels)
    {
      await ctx.Message.DeleteAsync();
      CheckPermissions(ctx);
      if (ctx.Guild.Roles.Any(role => role.Value.Name.Equals(name)))
        throw new InvalidOperationException($"{name} is already taken.");
      if (color == null)
        color = Program.GetColors().GetRandomColor();
      DiscordRole role = await ctx.Guild.CreateRoleAsync(name, null, color, null, false, $"Create {name} Campaign.");
      await ctx.Member.GrantRoleAsync(role, $"Creator of the {name} Campaign.");
      DiscordChannel campaignCategory = await ctx.Guild.CreateChannelCategoryAsync(name, GetCategoryOverwriteBuilders(ctx.Guild.EveryoneRole, role));
      DiscordChannel announcements = await ctx.Guild.CreateTextChannelAsync("announcements", campaignCategory, null, GetAnnouncementsOverwriteBuilders(ctx.Guild.EveryoneRole, ctx.Member, role), null, default, $"Create {name} Campaign.");
      Task[] tasks = new Task[] { };
      string[] channels = new string[] { "bot-commands", "chat", "dm-notes", "off-topic", "sending-stones", "whisper" };
      foreach (string channel in channels)
      {
        if (!excludeChannels.Any((excludedChannel) => excludedChannel == channel))
        {
          IEnumerable<DiscordOverwriteBuilder> overwrites = channel == "dm-notes" ? GetDMNotesOverwriteBuilders(ctx.Guild.EveryoneRole, ctx.Member, role) : null;
          tasks.Append(ctx.Guild.CreateTextChannelAsync(channel, campaignCategory, null, overwrites, null, default, $"Create {name} Campaign."));
        }
      }
      Task.WaitAll(tasks);
      await announcements.SendMessageAsync($"{ctx.Member.Mention} Campaign {name} has been created.");
    }

    [Command("create")]
    [RequirePrefixes("campaign")]
    [RequireRoles(RoleCheckMode.All, "DM")]
    [Hidden]
    public async Task CreateCampaign(CommandContext ctx, string name, params string[] excludeChannels)
    {
      await CreateCampaign(ctx, name, null, excludeChannels);
    }

    [Command("invite")]
    [RequireRoles(RoleCheckMode.All, "DM")]
    [RequirePrefixes("campaign")]
    [Description("Invite a user to a campaign.")]
    public async Task InviteToCampaign(CommandContext ctx, [Description("Name of the campaign.")] string name, [Description("Member to invite.")] DiscordMember member)
    {
      Messaging messaging = new Messaging(ctx);
      DiscordChannel channel = GetCampaignCategory(ctx, name);
      if (CheckOwnership(ctx, channel))
      {
        DiscordRole role = ctx.Guild.Roles.First(role => role.Value.Name.Equals(name)).Value;
        if (member.Roles.Contains(role))
        {
          await messaging.RespondContent()($"{member.Nickname} is already in {name}.");
          return;
        }
        IEnumerable<ulong> blacklistedInvites = await Program.GetB118DB().GetInviteBlacklistForUser(member.Id);
        if (blacklistedInvites.Contains(role.Id))
        {
          await messaging.RespondContent()($"{member.DisplayName} has already declined to join {name}.");
          return;
        }
        DiscordEmoji checkmark = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
        DiscordEmoji cancel = DiscordEmoji.FromName(ctx.Client, ":x:");
        DiscordMessage message = await member.SendMessageAsync($"{ctx.User.Username} has invited you to join the {name} campaign."
                                          + $"\nReact {checkmark} to accept the invitation."
                                          + $"\nReact {cancel} to decline this and further invites to this campaign.");
        await message.CreateReactionAsync(checkmark);
        await message.CreateReactionAsync(cancel);
        InteractivityExtension interactivity = ctx.Client.GetInteractivity();
        InteractivityResult<DSharpPlus.EventArgs.MessageReactionAddEventArgs> reaction = await interactivity.WaitForReactionAsync((e) =>
                  e.User.Id.Equals(member.Id) && e.Message.Id.Equals(message.Id) && (e.Emoji.Name.Equals(checkmark.Name) || e.Emoji.Name.Equals(cancel.Name)),
                  TimeSpan.FromSeconds(30));
        if (reaction.TimedOut)
        {
          await message.ModifyAsync($"~~{message.Content}~~\nInvite has expired.");
        }
        else
        {
          if (reaction.Result.Emoji.Name == checkmark.Name)
          {
            await member.GrantRoleAsync(role);
            string commandName = name.IndexOf(" ") >= 0 ? $"\"{name}\"" : name;
            await messaging.RespondContent(true, false)($"{member.Mention} has been added to {name}! You can always use `campaign leave {commandName}` to leave.");
          }
          else if (reaction.Result.Emoji.Name == cancel.Name)
          {
            await Program.GetB118DB().BlackListInvite(member.Id, role.Id);
            await member.SendMessageAsync($"You will no longer receive invites from {name}.");
          }
        }
      }
      else
      {
        await messaging.RespondContent(false)($"You have to be the DM to invite members to a campaign.");
      }
    }

    [Command("join")]
    [RequirePrefixes("campaign")]
    [Description("Request to join a campaign.")]
    public async Task JoinCampaign(CommandContext ctx, [Description("Name of the campaign to join.")] string name)
    {
      Messaging messaging = new Messaging(ctx);
      DiscordRole role = ctx.Guild.Roles.First(role => role.Value.Name.Equals(name)).Value;
      if (ctx.Member.Roles.Contains(role))
      {
        await messaging.RespondContent()($"You are already in {name}.");
        return;
      }
      IEnumerable<ulong> blacklistedRequests = await Program.GetB118DB().GetInviteBlacklistForCampaign(role.Id);
      DiscordChannel channel = GetCampaignCategory(ctx, name);
      DiscordMember dm = await GetOwner(ctx, channel);
      if (blacklistedRequests.Contains(ctx.Member.Id))
      {
        await messaging.RespondContent()($"{dm.DisplayName} has already rejected your request to join {name}.");
        return;
      }
      DiscordEmoji checkmark = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
      DiscordEmoji cancel = DiscordEmoji.FromName(ctx.Client, ":x:");
      DiscordMessage message = await dm.SendMessageAsync($"{ctx.User.Username} has requested to join your {name} campaign."
                                        + $"\nReact {checkmark} to accept the request."
                                        + $"\nReact {cancel} to reject this and further invites from this user.");
      await message.CreateReactionAsync(checkmark);
      await message.CreateReactionAsync(cancel);
      InteractivityExtension interactivity = ctx.Client.GetInteractivity();
      InteractivityResult<DSharpPlus.EventArgs.MessageReactionAddEventArgs> reaction = await interactivity.WaitForReactionAsync((e) =>
                e.User.Id.Equals(dm.Id) && e.Message.Id.Equals(message.Id) && (e.Emoji.Name.Equals(checkmark.Name) || e.Emoji.Name.Equals(cancel.Name)),
                TimeSpan.FromSeconds(30));
      if (reaction.TimedOut)
      {
        await message.ModifyAsync($"~~{message.Content}~~\nRequest has expired.");
      }
      else
      {
        if (reaction.Result.Emoji.Name == checkmark.Name)
        {
          await ctx.Member.GrantRoleAsync(role);
          string commandName = name.IndexOf(" ") >= 0 ? $"\"{name}\"" : name;
          await messaging.RespondContent(true, false)($"{ctx.Member.Mention} has been added to {name}! You can always use `campaign leave {commandName}` to leave.");
        }
        else if (reaction.Result.Emoji.Name == cancel.Name)
        {
          await Program.GetB118DB().BlackListRequest(ctx.Member.Id, role.Id);
          await dm.SendMessageAsync($"You will no longer receive requests from {ctx.Member.DisplayName}.");
        }
      }
    }

    [Command("remove")]
    [RequireRoles(RoleCheckMode.All, "DM")]
    [RequirePrefixes("campaign")]
    [Description("Remove a user from a campaign.")]
    public async Task RemoveFromCampaign(CommandContext ctx, [Description("Name of the campaign to remove a member from.")] string name, [Description("The member to remove.")] DiscordMember member, [Description("Whether or not to disallow this member from requesting to join again.")] bool bar = false)
    {
      Messaging messaging = new Messaging(ctx);
      DiscordChannel channel = GetCampaignCategory(ctx, name);
      if (CheckOwnership(ctx, channel))
      {
        DiscordRole role = ctx.Guild.Roles.First(role => role.Value.Name.Equals(name)).Value;
        await member.RevokeRoleAsync(role);
        if (bar)
        {
          await Program.GetB118DB().BlackListRequest(member.Id, role.Id);
        }
        await messaging.RespondContent()($"{member.Mention} has been removed from {name}!");
      }
      else
      {
        await messaging.RespondContent(false)($"You have to be the DM to remove members from a campaign.");
      }
    }

    [Command("leave")]
    [RequirePrefixes("campaign")]
    [Description("Leave a campaign.")]
    public async Task LeaveCampaign(CommandContext ctx, [Description("Name of the campaign to leave.")] string name, [Description("Whether or not to disallow future invites to this campaign.")] bool bar = false)
    {
      Messaging messaging = new Messaging(ctx);
      DiscordChannel channel = GetCampaignCategory(ctx, name);
      if (channel != null)
      {
        DiscordRole role = ctx.Guild.Roles.First(role => role.Value.Name.Equals(name)).Value;
        try
        {
          await ctx.Member.RevokeRoleAsync(role);
          if (bar)
          {
            await Program.GetB118DB().BlackListInvite(ctx.Member.Id, role.Id);
          }
          await ctx.Member.SendMessageAsync($"You have left the {name} campaign.");
          await ctx.Message.DeleteAsync();
        }
        catch (Exception)
        {
          await messaging.RespondContent()($"You are not a member of {name}.");
        }
      }
      else
      {
        await messaging.RespondContent()($"Campaign {name} doesn't exist.");
      }
    }

    [Command("delete")]
    [RequireRoles(RoleCheckMode.All, "DM")]
    [RequirePrefixes("campaign")]
    [Description("Delete a campaign.")]
    public async Task DeleteCampaign(CommandContext ctx, [Description("Name of the campaign to delete.")] string name)
    {
      DiscordChannel channel = GetCampaignCategory(ctx, name);
      if (CheckOwnership(ctx, channel))
      {
        KeyValuePair<ulong, DiscordRole> role = ctx.Guild.Roles.First(role => role.Value.Name.Equals(name));
        DiscordMember member = ctx.Member;
        try
        {
          await ctx.Message.DeleteAsync();
        }
        catch (Exception) { }
        foreach (DiscordChannel child in channel.Children)
        {
          await child.DeleteAsync($"Deleting {name} Campaign");
        }
        await role.Value.DeleteAsync();
        await member.SendMessageAsync($"Campaign {name} has been deleted.");
        await channel.DeleteAsync();
      }
      else
      {
        Messaging messaging = new Messaging(ctx);
        await messaging.RespondContent()($"You don't have permission to delete the {name} campaign.");
      }
    }

    [Command("rename")]
    [RequireRoles(RoleCheckMode.All, "DM")]
    [RequirePrefixes("campaign")]
    [Description("Rename a campaign.")]
    public async Task RenameCampaign(CommandContext ctx, [Description("Name of the campaign to rename.")] string currentName, [Description("New name for the campaign.")] string nextName)
    {
      Messaging messaging = new Messaging(ctx);
      DiscordChannel channel = GetCampaignCategory(ctx, currentName);
      if (CheckOwnership(ctx, channel))
      {
        DiscordChannel nextChannel = GetCampaignCategory(ctx, nextName);
        KeyValuePair<ulong, DiscordRole> nextRole = ctx.Guild.Roles.FirstOrDefault(role => role.Value.Name.Equals(nextName));
        if (nextChannel == null && nextRole.Value == null)
        {
          Action<DSharpPlus.Net.Models.ChannelEditModel> action = new Action<DSharpPlus.Net.Models.ChannelEditModel>((target) =>
                {
                  target.Name = nextName;
                });
          await channel.ModifyAsync(action);
          KeyValuePair<ulong, DiscordRole> role = ctx.Guild.Roles.FirstOrDefault(role => role.Value.Name.Equals(currentName));
          await role.Value.ModifyAsync(nextName);
          await messaging.RespondContent()($"{currentName} has been renamed to {nextName}");
        }
      }
      else
      {
        await messaging.RespondContent()("You must be the DM to rename a channel.");
      }
    }
  }
}
