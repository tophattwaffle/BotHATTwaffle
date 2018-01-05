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
using BotHATTwaffle.Modules.Json;
using System.Text.RegularExpressions;

namespace BotHATTwaffle.Modules
{
    public class ModerationServices
    {
        public List<UserData> muteList = new List<UserData>();
        public string[] TestInfo { get; set; }
        DataServices _dataServices;

        public ModerationServices(DataServices dataServices)
        {
            _dataServices = dataServices;
        }

        public void Cycle()
        {
            //Check for unmutes
            foreach (UserData u in muteList.ToList())
            {
                if (!(u.User).Roles.Contains(_dataServices.MuteRole))
                {
                    _dataServices.ChannelLog($"{u.User} was manually unmuted from someone removing the role.","Removing them from the mute list.");
                    muteList.Remove(u);
                }
                if (u.CanUnmute())
                {
                    
                    u.User.RemoveRoleAsync(_dataServices.MuteRole);
                    u.User.SendMessageAsync("You have been unmuted!");
                    muteList.Remove(u);
                    _dataServices.ChannelLog($"{u.User} Has been unmuted.");
                    Task.Delay(1000);
                }
            }
        }

        public void AddMute(SocketGuildUser inUser, DateTime inUnmuteTime)
        {
            Console.WriteLine($"ADD MUTE {inUser} {inUnmuteTime}");
            muteList.Add(new UserData() {
                User = inUser,
                UnmuteTime = inUnmuteTime
            });
        }
    }

    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        private ModerationServices _mod;
        private LevelTesting _levelTesting;
        private DataServices _dataServices;

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
        public async Task RconAsync(string serverString = null, [Remainder]string command = null)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("```This command can not be used in a DM```");
                await _dataServices.ChannelLog($"{Context.User} was trying to rcon from DM.", $"{command} was trying to be sent to {serverString}");
                return;
            }

            if ((Context.User as SocketGuildUser).Roles.Contains(_dataServices.ModRole) || (Context.User as SocketGuildUser).Roles.Contains(_dataServices.RconRole))
            {
                //Display list of servers
                if (serverString == null && command == null)
                {
                    await ReplyAsync("",false, _dataServices.GetAllServers());
                    return;
                }

                //Return if we use these commands.
                if (command.ToLower().Contains("rcon_password") || command.ToLower().Contains("exit"))
                {
                    await ReplyAsync("```This command cannot be run from here. Ask TopHATTwaffle to do it.```");
                    await _dataServices.ChannelLog($"{Context.User} was trying to run a blacklisted command", $"{command} was trying to be sent to {serverString}");
                    return;
                }

                var server = _dataServices.GetServer(serverString);
                string reply = null;
                try
                {
                    if (server != null)
                        reply = await _dataServices.RconCommand(command, server);

                    if (reply.Length > 1880)
                        reply = $"{reply.Substring(0, 1880)}\n[OUTPUT OMITTED...]";
                    
                    //Remove log messages from log
                    string[] replyArray = reply.Split(
                    new[] { "\r\n", "\r", "\n" },
                    StringSplitOptions.None
                    );
                    reply = string.Join("\n", replyArray.Where(x => !x.Trim().StartsWith("L ")));
                    reply = reply.Replace("discord.gg", "discord,gg").Replace(server.Password,"[PASSWORD HIDDEN]");
                }
                catch { }

                if (reply == "HOST_NOT_FOUND")
                    await ReplyAsync($"```Cannot send command because the servers IP address could not be found\nThis is a probably a DNS issue.```");
                else if (server == null)
                    await ReplyAsync($"```Cannot send command because the server could not be found.\nIs it in the json?.```");
                else
                {
                    if (command.Contains("sv_password"))
                    {
                        await Context.Message.DeleteAsync(); //Message was setting password, delete it.
                        await ReplyAsync($"```Command Sent to {server.Name}\nA password was set on the server.```");
                        await _dataServices.ChannelLog($"{Context.User} Sent RCON command", $"A password command was sent to: {server.Address}");
                    }
                    else
                    {
                        await ReplyAsync($"```{command} sent to {server.Name}\n{reply}```");
                        await _dataServices.ChannelLog($"{Context.User} Sent RCON command", $"{command} was sent to: {server.Address}\n{reply}");
                    }
                }
            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} is trying to rcon from the bot. They tried to send {serverString} {command}");
                await ReplyAsync("```You cannot use this command with your current permission level!```");
            }
        }

        [Command("playtest")]
        [Summary("`>playtest [pre/start/post/scramble/pause/unpause] [Optional serverPrefix]` Playtest Functions")]
        [Remarks("`>playtest pre` Sets the testing config then reloads the map to clear cheats." +
            "\n`>playtest start` Starts the playtest, starts a demo recording, then tells the server it is live." +
            "\n`>playtest post` Starts postgame config. Gets the playtest demo and BSP file. Places it into the public DropBox folder." +
            "\n`>playtest scramble` or `>p s` Scrambles teams." +
            "\n`>playtest pause` or `>p p` Pauses playtest." +
            "\n`>playtest unpause` or `>p u` Unpauses playtest." +
            "\nIf a server prefix is provided, commands will go to that server. If no server is provided, the event server will be used. `>p start eus`")]
        [Alias("p")]
        public async Task PlaytestAsync(string action, string serverStr = "nothing")
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("**This command can not be used in a DM**");
                return;
            }

            if ((Context.User as SocketGuildUser).Roles.Contains(_dataServices.ModRole))
            {
                if(_levelTesting.currentEventInfo[0] == "NO_EVENT_FOUND")
                {
                    await ReplyAsync("```Cannot use this command unless a test is scheduled```");
                    return;
                }
                string config = null;
                JsonServer server = null;
                //Get the right server. If null, use the server in the event info. Else we'll use what was provided.
                if (serverStr == "nothing")
                    server = _dataServices.GetServer(_levelTesting.currentEventInfo[10].Substring(0, 3));
                else
                    server = _dataServices.GetServer(serverStr);

                if (_levelTesting.currentEventInfo[7].ToLower() == "competitive" || _levelTesting.currentEventInfo[7].ToLower() == "comp")
                    config = _dataServices.compConfig;
                else
                    config = _dataServices.casualConfig; //If not comp, casual.

                if (action.ToLower() == "pre")
                {
                    _mod.TestInfo = _levelTesting.currentEventInfo; //Set the test info so we can use it when getting the demo back.
                    var result = Regex.Match(_levelTesting.currentEventInfo[6], @"\d+$").Value;

                    await _dataServices.ChannelLog($"Playtest Prestart on {server.Name}", $"exec {config}" +
                        $"\nhost_workshop_map {result}");

                    await _dataServices.RconCommand($"exec {config}", server);
                    await Task.Delay(1000);
                    await _dataServices.RconCommand($"host_workshop_map {result}", server);
                    await ReplyAsync($"```Playtest Prestart on {server.Name}" +
                        $"\nexec {config}" +
                        $"\nhost_workshop_map {result}```");
                }
                else if (action.ToLower() == "start")
                {
                    _mod.TestInfo = _levelTesting.currentEventInfo; //Set the test info so we can use it when getting the demo back.

                    DateTime testTime = Convert.ToDateTime(_levelTesting.currentEventInfo[1]);
                    string demoName = $"{testTime.ToString("MM_dd_yyyy")}_{_levelTesting.currentEventInfo[2].Substring(0, _levelTesting.currentEventInfo[2].IndexOf(" "))}_{_levelTesting.currentEventInfo[7]}";

                    await _dataServices.ChannelLog($"Playtest Start on {server.Name}", $"exec {config}" +
                        $"\ntv_record {demoName}" +
                        $"\nsay Playtest of {_levelTesting.currentEventInfo[2].Substring(0, _levelTesting.currentEventInfo[2].IndexOf(" "))} is now live! Be respectiful and GLHF!");

                    await ReplyAsync($"```Playtest Start on {server.Name}" +
                        $"\nexec {config}" +
                        $"\ntv_record {demoName}```");

                    await _dataServices.RconCommand($"exec {config}", server);
                    await Task.Delay(3250);
                    await _dataServices.RconCommand($"tv_record {demoName}", server);
                    await Task.Delay(1000);
                    await _dataServices.RconCommand($"say Demo started! {demoName}", server);
                    await Task.Delay(1000);
                    await _dataServices.RconCommand($"say Playtest of {_levelTesting.currentEventInfo[2].Substring(0, _levelTesting.currentEventInfo[2].IndexOf(" "))} is now live! Be respectful and GLHF!", server);
                    await Task.Delay(1000);
                    await _dataServices.RconCommand($"say Playtest of {_levelTesting.currentEventInfo[2].Substring(0, _levelTesting.currentEventInfo[2].IndexOf(" "))} is now live! Be respectful and GLHF!", server);
                    await Task.Delay(1000);
                    await _dataServices.RconCommand($"say Playtest of {_levelTesting.currentEventInfo[2].Substring(0, _levelTesting.currentEventInfo[2].IndexOf(" "))} is now live! Be respectful and GLHF!", server);
                }
                else if (action.ToLower() == "post")
                {
                    await ReplyAsync($"```Playtest post started. Begin feedback!```");

#pragma warning disable CS4014 //Lets do all of the post game tasks and not wait. This is so the bot won't skip a heartbeat.
                    PostTasks(server);
#pragma warning restore CS4014

                    await _dataServices.ChannelLog($"Playtest Post on {server.Name}", $"exec {_dataServices.postConfig}" +
                        $"\nsv_voiceenable 0" +
                        $"\nGetting Demo and BSP file and moving into DropBox");
                }
                else if(action.ToLower() == "scramble" || action.ToLower() == "s")
                {
                    await _dataServices.RconCommand($"mp_scrambleteams 1", server);
                    await ReplyAsync($"```Playtest Scramble on {server.Name}" +
                        $"\nmp_scrambleteams 1```");
                    await _dataServices.ChannelLog($"Playtest Scramble on {server.Name}", $"mp_scrambleteams 1");
                }
                else if (action.ToLower() == "pause" || action.ToLower() == "p")
                {
                    await _dataServices.RconCommand(@"mp_pause_match; say Pausing Match", server);
                    await ReplyAsync($"```Playtest Scramble on {server.Name}" +
                        $"\nmp_pause_match```");
                    await _dataServices.ChannelLog($"Playtest Pause on {server.Name}", $"mp_pause_match");
                }
                else if (action.ToLower() == "unpause" || action.ToLower() == "u")
                {
                    await _dataServices.RconCommand(@"mp_unpause_match; say Unpausing Match", server);
                    await ReplyAsync($"```Playtest Unpause on {server.Name}" +
                        $"\nmp_unpause_match```");
                    await _dataServices.ChannelLog($"Playtest Unpause on {server.Name}", $"mp_unpause_match");
                }
                else
                {
                    await ReplyAsync($"Bad input, please try:" +
                        $"\n`pre`" +
                        $"\n`start`" +
                        $"\n`post`" +
                        $"\n`scramble` or `s`" +
                        $"\n`pause` or `p`" +
                        $"\n`unpause` or `u`");
                }

            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} is trying to use the playtest command.");
                await ReplyAsync("```You cannot use this command with your current permission level!```");
            }
        }

        private async void PostTasks(JsonServer server)
        {
            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Download Playtest Demo for {_mod.TestInfo[2]}",
                IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png",
            };

            var builder = new EmbedBuilder()
            {
                Author = authBuilder,
                Url = "http://demos.tophattwaffle.com",
                Title = "Download Here",
                ThumbnailUrl = _mod.TestInfo[4],
                Color = new Color(243, 128, 72),
                Description = $"You can get the demo for this playtest by clicking above!" +
                $"\n\n*Thanks for testing with us!*" +
                $"\n\n[Map Images]({_mod.TestInfo[5]}) | [Schedule a Playtest](https://www.tophattwaffle.com/playtesting/) | [View Testing Calendar](http://playtesting.tophattwaffle.com)"

            };

            var result = Regex.Match(_mod.TestInfo[6], @"\d+$").Value;
            await _dataServices.RconCommand($"host_workshop_map {result}", server);
            await Task.Delay(15000);
            await _dataServices.RconCommand($"exec {_dataServices.postConfig}; say Please join the Level Testing voice channel for feedback!", server);
            await Task.Delay(3000);
            await _dataServices.RconCommand($"say Please join the Level Testing voice channel for feedback!", server);
            await Task.Delay(3000);
            await _dataServices.RconCommand($"say Please join the Level Testing voice channel for feedback!", server);
            await Task.Delay(3000);
            await _dataServices.RconCommand($"say Please join the Level Testing voice channel for feedback!", server);
            await Task.Delay(3000);
            await _dataServices.RconCommand($"say Please join the Level Testing voice channel for feedback!", server);

#pragma warning disable CS4014 // Program can hang if we await this while downloading the files. So just don't wait.
            _dataServices.GetPlayTestFiles(_mod.TestInfo, server);
#pragma warning restore CS4014

            var splitUser = _mod.TestInfo[3].Split('#');

            try
            {
                //Try to DM them the information to get their demos.
                await Program._client.GetUser(splitUser[0], splitUser[1]).SendMessageAsync("", false, builder);
            }
            catch
            {
                try
                {
                    //If they don't accepts DMs, tag them in level testing.
                    await _dataServices.testingChannel.SendMessageAsync($"{Program._client.GetUser(splitUser[0], splitUser[1]).Mention} You can download your demo here:");
                }
                catch
                {
                    //If it cannot get the name from the event info, nag them in level testing.
                    await _dataServices.testingChannel.SendMessageAsync($"Hey {_mod.TestInfo[3]}! Next time you submit for a playtest, make sure to include your full Discord name so I can mention you. You can download your demo here:");
                }
            }
            await _dataServices.testingChannel.SendMessageAsync($"", false, builder);
        }

        [Command("shutdown")]
        [Summary("`>shutdown [type]` shuts down or restarts bot services")]
        [Remarks("Requirements: Moderator Role. `s` for shutdown `r` for restart")]
        public async Task ShutdownAsync(char type)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("**This command can not be used in a DM**");
                return;
            }

            if ((Context.User as SocketGuildUser).Roles.Contains(_dataServices.ModRole))
            {
                await Context.Message.DeleteAsync();
                if(type == 's')
                {
                    await _dataServices.ChannelLog($"Shutting down! Invoked by {Context.Message.Author}");
                    await Task.Delay(1000);
                    Environment.Exit(0);
                }
                if(type == 'r')
                {
                    await _dataServices.ChannelLog($"Restarting! Invoked by {Context.Message.Author}");
                    await Task.Delay(1000);
                    Process secondProc = new Process();
                    secondProc.StartInfo.FileName = "BotHATTwaffle.exe";
                    secondProc.Start();
                    Environment.Exit(0);
                }
            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} is trying to shutdown the bot.");
                await ReplyAsync("```You cannot use this command with your current permission level!```");
            }
        }

        [Command("reload")]
        [Summary("`>reload]` Reloads data from settings files.")]
        [Remarks("Requirements: Moderator Role.")]
        public async Task ReloadAsync(string arg = null)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("**This command can not be used in a DM**");
                return;
            }

            if ((Context.User as SocketGuildUser).Roles.Contains(_dataServices.ModRole))
            {
                if (arg == "dump")
                {
                    await Context.Message.DeleteAsync();
                    var lines = _dataServices.config.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());
                    var reply = string.Join(Environment.NewLine, lines);
                    reply = reply.Replace((_dataServices.config["botToken"]), "[TOKEN HIDDEN]");
                    try
                    {
                        await Context.Message.Author.SendMessageAsync($"```{reply}```");
                    }
                    catch { }//Do nothing if we can't DM.
                }
                else
                {
                    await ReplyAsync("```Reloading Data!```");
                    await _dataServices.ChannelLog($"{Context.User} reloaded bot data!");
                    _dataServices.ReadData();
                }
            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} is trying to shutdown the bot.");
                await ReplyAsync("```You cannot use this command with your current permission level!```");
            }
        }


        //TODO: Announce Command
        //Posts announcement to Announcements channel. Will need to be responsive
        //so it can prompt people for: title, description, ect... Then use embed builder to post it.
        //Have a time limit on the module so it can auto remove after X amount of time.

        [Command("mute")]
        [Summary("`>mute [@user] [duration] [Optional Reason]` Mutes someone.")]
        [Remarks("Requirements: Moderator Role" +
            "\n`>mute @person 15 Being a mean person` will mute them for 15 minutes, with the reason \"Being a mean person\"")]
        [Alias("m")]
        public async Task MuteAsync(SocketGuildUser user, int durationInMinutes = 5, [Remainder]string reason = "No reason provided")
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("***This command can not be used in a DM***");
                return;
            }

            if ((Context.User as SocketGuildUser).Roles.Contains(_dataServices.ModRole))
            {
                DateTime unmuteTime = DateTime.Now.AddMinutes(durationInMinutes);
                _mod.AddMute(user, unmuteTime);
                await _dataServices.ChannelLog($"{Context.User} muted {user}", $"They were muted for {durationInMinutes} minutes because:\n{reason}.");
                await user.AddRoleAsync(_dataServices.MuteRole);
                await user.SendMessageAsync($"You were muted for {durationInMinutes} minutes because:\n{reason}.\n");
            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} is trying to mute {user} without the right permissions!");
                await ReplyAsync("```You cannot use this command with your current permission level!```");
            }
        }

        [Command("ClearReservations")]
        [Summary("`>cr` Clears all server reservations")]
        [Remarks("Requirements: Moderator Role")]
        [Alias("cr")]
        public async Task ClearReservationsAsync(string serverStr = null)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("***This command can not be used in a DM***");
                return;
            }

            if ((Context.User as SocketGuildUser).Roles.Contains(_dataServices.ModRole))
            {
                if(serverStr == null)
                    await _levelTesting.ClearServerReservations();
                else
                    await _levelTesting.ClearServerReservations(serverStr);

                await ReplyAsync("", false, _levelTesting.DisplayServerReservations());
            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} is trying to clear reservations without the right permissions!");
                await ReplyAsync("```You cannot use this command with your current permission level!```");
            }
        }

    }
}