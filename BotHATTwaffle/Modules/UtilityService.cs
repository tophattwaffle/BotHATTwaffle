using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Discord.WebSocket;
using System.Threading;

namespace BotHATTwaffle.Modules
{
    public class UtilityService
    {
        public UtilityService()
        {
            
        }

        public void Cycle()
        {
            //We'll loop utility things here each tick
        }
    }

    public class UtilityModule : ModuleBase<SocketCommandContext>
    {
        private readonly UtilityService _utility;
        DataServices _dataServices;

        public UtilityModule(UtilityService utility, DataServices dataServices)
        {
            _dataServices = dataServices;
            _utility = utility;
            
        }

        [Command("ping")]
        [Summary("`>ping` Replies with a message")]
        [Remarks("It's a ping command.")]
        public async Task PingAsync()
        {
            var replyTime = Program._client.Latency;
            var builder = new EmbedBuilder()
            {
                Color = new Color(47, 111, 146),
                Description = $"*Do you like waffles?*" +
                $"\nIt took me **{replyTime}ms** to reach the Discord API."
            };
            await ReplyAsync("", false, builder);
        }

        [Command("roleme")]
        [Summary("`>roleme [rolename]` Toggles roles on a user")]
        [Remarks("This will let you add roles to yourself. Typically for saying you have a skill like 3D Modeling, or level design." +
            "\n__Channel names are case sensitive!!!__\n" +
            "You can type `>roleme` to show all roles available")]
        public async Task RolemeAsync([Remainder]string inRoleStr = null)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("**This command can not be used in a DM**");
                return;
            }

            var user = Context.User as SocketGuildUser;
            var inRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == inRoleStr);

            //Display roles, or modify role state
            if (inRoleStr == null)
            {
                await ReplyAsync($"*Remember roles are case sensitive.* Valid roles are:```\n{string.Join("\n", _dataServices.roleMeWhiteList)}```");
            }
            else
            {
                //Validate that we can apply the role
                Boolean valid = false;
                foreach (string s in _dataServices.roleMeWhiteList)
                {
                    if (inRoleStr.Contains(s))
                    {
                        valid = true;
                        break;
                    }
                }

                if (valid)
                {
                    if (user.Roles.Contains(inRole))
                    {
                        await _dataServices.ChannelLog($"{Context.User} has removed {inRoleStr} role from themselves.");
                        await ReplyAsync($"{Context.User.Mention} lost the **{inRoleStr}** role.");
                        await (user as IGuildUser).RemoveRoleAsync(inRole);
                    }
                    else
                    {
                        await _dataServices.ChannelLog($"{Context.User} has assigned themselves the role of {inRoleStr}");
                        await ReplyAsync($"{Context.User.Mention} now has the role **{inRoleStr}**. Enjoy the flair!");
                        await (user as IGuildUser).AddRoleAsync(inRole);
                    }
                }
                else
                {
                    await ReplyAsync($"{Context.User.Mention} you cannot assign yourself the role of **{inRoleStr}** because it does not exist, " +
                        $"or it is not allowed.");
                    await _dataServices.ChannelLog($"{Context.User} attempted to roleMe the role of: {inRoleStr} and it failed. Either due to the " +
                        $"role not existing, or they tried to use a role that isn't in the white list.");
                }
            }
        }
    }
}
