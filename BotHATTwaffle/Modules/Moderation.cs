﻿using Discord;
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
        public List<UserData> muteList = new List<UserData>();
        SocketRole muteRole;
        SocketRole modRole;
        public string modRoleStr;
        public string mutedRoleNameStr;

        public ModerationServices()
        {
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
        private readonly DataServices _dataServices;

        public ModerationModule(ModerationServices mod, LevelTesting levelTesting, DataServices dataServices)
        {
            _dataServices = dataServices;
            _levelTesting = levelTesting;
            _mod = mod;
        }

        [Command("rcon")]
        [Summary("`>rcon [server] [command]` Sends rcon command to server.")]
        [Remarks("Requirements: Moderator Role. Sends rcon command to the desired server. Use the server 3 letter code (eus) to pick the server. If " +
            "the command returns output it will be displayed. Some commands do not have output.")]
        [Alias("r")]
        public async Task RconAsync(string serverString, [Remainder]string command)
        {
            _mod.SetModRole(Context.Guild.Roles.FirstOrDefault(x => x.Name == _mod.modRoleStr));
            if ((Context.User as SocketGuildUser).Roles.Contains(_mod.GetModRole()))
            {
                var server = _dataServices.GetServer(serverString);
                string reply = null;
                try
                {
                    if (server != null)
                        reply = await _dataServices.RconCommand(command, server);
                }
                catch { }

                if(reply == "HOST_NOT_FOUND")
                    await ReplyAsync($"```Cannot send command because the servers IP address could not be found\nThis is a probably a DNS issue.```");
                else if(server == null)
                    await ReplyAsync($"```Cannot send command because the server could not be found.\nIs it in the json?.```");
                else
                    await ReplyAsync($"```Command Sent to {server.Name}\n{reply}```");
                
            }
            else
            {
                await Program.ChannelLog($"{Context.User} is trying to rcon from the bot.");
                await ReplyAsync("You cannot use this command with your current permission level!");
            }
        }

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
                    await Program.ChannelLog($"Shutting down! Invoked by {Context.Message.Author.Username}");
                    Environment.Exit(0);
                }
                if(type == 'r')
                {
                    await Program.ChannelLog($"Restarting! Invoked by {Context.Message.Author.Username}");
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


        //TODO: Announce Command
        //Posts announcement to Announcements channel. Will need to be responsive
        //so it can prompt people for: title, description, ect... Then use embed builder to post it.
        //Have a time limit on the module so it can auto remove after X amount of time.

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