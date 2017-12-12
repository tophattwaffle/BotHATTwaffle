using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
                    Program.ChannelLog($"{u.user.Username} Has been unmuted.");
                    Task.Delay(1000);
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


        //TODO: Rewrite these are the settings module is rewritten. Get the values from there instead of from the mute command.
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
        private readonly LevelTesting _levelTesting;

        public ModerationModule(ModerationServices mod, LevelTesting levelTesting)
        {
            _levelTesting = levelTesting;
            _mod = mod;
        }

        //TODO: Shutdown Command.
        //Safely shutdown the bot. Deletes the Playtest Announcement message so it can be rebuilt next load.
        //This will eventually save any pending information to a file
        #region shutdown
        [Command("shutdown")]
        [Summary("`>shutdown [type]` shuts down or restarts bot services")]
        [Remarks("Requirements: Moderator Role. `s` for shutdown `r` for restart")]
        public async Task ShutdownAsync(char type)
        {
            _mod.SetModRole(Context.Guild.Roles.FirstOrDefault(x => x.Name == _mod.modRoleStr));

            if ((Context.User as SocketGuildUser).Roles.Contains(_mod.GetModRole()))
            {
                await Context.Message.DeleteAsync();
                await _levelTesting.announceMessage.DeleteAsync();

                if(type == 's')
                {
                    Environment.Exit(0);
                }
                if(type == 'r')
                {
                    Process secondProc = new Process();
                    secondProc.StartInfo.FileName = "BotHATTwaffle.exe";
                    secondProc.Start();
                    Environment.Exit(0);
                }
            }
            else
            {
                await Program.ChannelLog($"{Context.User} is trying to shutdown the bot.");
                await ReplyAsync("You cannot use this command with your current permission level!");
            }
        }
        #endregion

        //TODO: Announce Command
        //Posts announcement to Announcements channel. Will need to be responsive
        //so it can prompt people for: title, description, ect... Then use embed builder to post it.
        //Have a time limit on the module so it can auto remove after X amount of time.
        #region Mute
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
        #endregion
    }
}