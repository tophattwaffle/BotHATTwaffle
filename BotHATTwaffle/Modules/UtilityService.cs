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
            var replyTime = Program.Client.Latency;
            var builder = new EmbedBuilder()
            {
                Color = new Color(47, 111, 146),
                Description = $"*Do you like waffles?*" +
                $"\nIt took me **{replyTime}ms** to reach the Discord API."
            };
            await ReplyAsync("", false, builder);
        }

        [Command("roleme")]
        [Summary("`>roleme [role names]` Toggles roles on a user")]
        [Remarks("This will let you add roles to yourself. Typically for saying you have a skill like 3D Modeling, or level design." +
            "\nYou can put multiple roles into one command to get multiple at one time. Example: `>roleme blender level design programmer`" +
            "\nYou can type `>roleme` to show all roles available")]
        public async Task RolemeAsync([Remainder]string inRoleStr = "display")
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("**This command can not be used in a DM**");
                return;
            }

            var user = Context.User as SocketGuildUser;

            //Display roles, or modify role state
            if (inRoleStr.ToLower() == "display")
            {
                await ReplyAsync($"Valid roles are:```\n{string.Join("\n", _dataServices.RoleMeWhiteList)}```");
            }
            else
            {
                //Validate that we can apply the role
                Boolean valid = false;
                string reply = null;
                foreach (string s in _dataServices.RoleMeWhiteList)
                {
                    if (inRoleStr.ToLower().Contains(s.ToLower()))
                    {
                        valid = true; //We applied at least 1 role.

                        var inRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == s);

                        if (user.Roles.Contains(inRole))
                        {
                            reply += $"{Context.User.Username} lost the **{inRole}** role.\n";
                            await (user as IGuildUser).RemoveRoleAsync(inRole);
                        }
                        else
                        {
                            reply += $"{Context.User.Username} now has the role **{inRole}**. Enjoy the flair!\n";
                            await (user as IGuildUser).AddRoleAsync(inRole);
                        }
                    }
                }

                if(valid)
                {
                    await ReplyAsync($"{reply}");
                    await _dataServices.ChannelLog($"{Context.User}\n{reply}");
                }
                else
                {
                    await ReplyAsync($"{Context.User.Mention}\n```You cannot assign yourself the role of **{inRoleStr}** because it does not exist, " +
                        $"or it is not allowed.```");
                    await _dataServices.ChannelLog($"{Context.User} attempted to roleMe the role of: {inRoleStr} and it failed. Either due to the " +
                        $"role not existing, or they tried to use a role that isn't in the white list.");
                }
            }
        }
    }
}
