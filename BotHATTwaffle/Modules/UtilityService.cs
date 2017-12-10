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

        string[] roleMeWhiteList;

        public UtilityModule(UtilityService utility)
        {
            _utility = utility;
            if (Program.config.ContainsKey("roleMeWhiteListCSV"))
                roleMeWhiteList = (Program.config["roleMeWhiteListCSV"]).Split(',');
        }

        [Command("ping")]
        [Summary("`>ping` Replies with a message")]
        [Remarks("It's a ping command.")]
        [Alias("p")]
        public async Task PingAsync()
        {
            var replyTime = Program._client.Latency;
            var builder = new EmbedBuilder()
            {
                Color = new Color(47, 111, 146),
                Description = $"*Do you like waffles?*" +
                $"\nIt took me **{replyTime}ms** to get back to you."
            };
            await ReplyAsync("", false, builder);
        }

        [Command("roleme")]
        [Summary("`>roleme [rolename]` Toggles roles on a user")]
        [Remarks("This will let you add roles to yourself. Typically for saying you have a skill like 3D Modeling, or level design." +
            "\n__Channel names are case sensative!!!__\n" +
            "You can type `>roleme display` to show all roles avaiable")]
        public async Task RolemeAsync([Remainder]string inRoleStr)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("**This command can not be used in a DM**");
                return;
            }

            var user = Context.User as SocketGuildUser;
            var inRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == inRoleStr);

            //Display roles, or modify role state
            if (inRoleStr == "display")
            {
                await ReplyAsync($"*Remember roles are case sensitive.* Valid roles are:```\n{string.Join("\n", roleMeWhiteList)}```");
            }
            else
            {
                //Validate that we can apply the role
                Boolean valid = false;
                foreach (string s in roleMeWhiteList)
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
                        await Program.ChannelLog($"{Context.User} has removed {inRoleStr} role from themselves.");
                        await ReplyAsync($"{Context.User.Mention} lost the **{inRoleStr}** role.");
                        await (user as IGuildUser).RemoveRoleAsync(inRole);
                    }
                    else
                    {
                        await Program.ChannelLog($"{Context.User} has assigned themself the role of {inRoleStr}");
                        await ReplyAsync($"{Context.User.Mention} now has the role **{inRoleStr}**. Enjoy the flair!");
                        await (user as IGuildUser).AddRoleAsync(inRole);
                    }
                }
                else
                {
                    await ReplyAsync($"{Context.User.Mention} you cannot assign yourself the role of **{inRoleStr}** because it does not exist, " +
                        $"or it is not allowed.");
                    await Program.ChannelLog($"{Context.User} attempted to roleMe the role of: {inRoleStr} and it failed. Either due to the " +
                        $"role not existing, or they tried to use a role that isn't in the whitelist.");
                }
            }
        }
    }
}
