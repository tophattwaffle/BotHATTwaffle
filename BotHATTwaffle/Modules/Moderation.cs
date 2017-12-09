using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;

namespace BotHATTwaffle.Modules
{
    public class ModerationServices
    {
        public List<UserData> muteList;
        SocketRole muteRole;
        SocketRole modRole;
        public string modRoleStr;
        public string mutedRoleNameStr;

        public ModerationServices()
        {
            muteList = new List<UserData>();

            if (Program.config.ContainsKey("moderatorRoleName"))
                modRoleStr = (Program.config["moderatorRoleName"]);
            
            if (Program.config.ContainsKey("mutedRoleName"))
                mutedRoleNameStr = (Program.config["mutedRoleName"]);
        }

        public void Cycle()
        {
            //Check for unmutes
            foreach (UserData u in muteList.ToList())
            {
                if(u.CanUnmute())
                {
                    u.user.RemoveRoleAsync(muteRole);
                    u.user.SendMessageAsync("You have been unmuted!");
                    muteList.Remove(u);
                    Thread.Sleep(1000);
                }
            }
        }

        public void AddMute(SocketGuildUser inUser, DateTime inUnmuteTime)
        {
            Console.WriteLine($"ADD MUTE {inUser.Username} {inUnmuteTime}");
            muteList.Add(new UserData() {
                user = inUser,
                unmuteTime = inUnmuteTime
            });
        }

        public void SetMuteRole(SocketRole inMuteRole)
        {
            muteRole = inMuteRole;
        }

        public SocketRole GetMuteRole()
        {
            return muteRole;
        }

        public void SetModRole(SocketRole inModRole)
        {
            modRole = inModRole;
        }

        public SocketRole GetModRole()
        {
            return modRole;
        }
    }

    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        private readonly ModerationServices _mod;

        public ModerationModule(ModerationServices mod)
        {
            _mod = mod;
        }

        [Command("mute")]
        [Summary("`>mute [@user]` Mutes someone.")]
        [Remarks("Requirements: Moderator Role")]
        [Alias("m")]
        public async Task MuteAsync(SocketGuildUser user, int durationInMinutes = 5, [Remainder]string reason = "No reason provided")
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("***This command can not be used in a DM***");
                return;
            }
            _mod.SetMuteRole(Context.Guild.Roles.FirstOrDefault(x => x.Name == _mod.mutedRoleNameStr));
            _mod.SetModRole(Context.Guild.Roles.FirstOrDefault(x => x.Name == _mod.modRoleStr));

            if ((Context.User as SocketGuildUser).Roles.Contains(_mod.GetModRole()))
            {
                DateTime unmuteTime = DateTime.Now.AddMinutes(durationInMinutes);
                _mod.AddMute(user, unmuteTime);
                await Program.ChannelLog($"{Context.User} muted {user}", $"They were muted for {durationInMinutes} minutes because:\n{reason}.");
                await user.AddRoleAsync(_mod.GetMuteRole());
                await user.SendMessageAsync($"You were muted for {durationInMinutes} minutes because:\n{reason}.\n");
            }
            else
            {
                await Program.ChannelLog($"{Context.User} is trying to mute {user} without the right permissions!");
                await ReplyAsync("You cannot use this command with your current permission level!");
            }
        }
    }
}