using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotHATTwaffle.Commands.Preconditions;
using BotHATTwaffle.Models;
using BotHATTwaffle.Services;
using BotHATTwaffle.Services.Playtesting;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Summer;

namespace BotHATTwaffle.Commands
{
    public class PlaytestingModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly PlaytestingService _playtesting;
        private readonly DataService _dataService;
        private readonly WorkshopItem _wsItem = new WorkshopItem();

        public PlaytestingModule(DiscordSocketClient client, PlaytestingService playtesting, DataService dataService)
        {
            _client = client;
            _playtesting = playtesting;
            _dataService = dataService;
        }

        [Command("PublicServer")]
        [Summary("Reserves a public server under the invoking user for personal testing purposes.")]
        [Remarks("A reservation lasts 2 hours. A Workshop ID can be included in order to have that map automatically hosted. " +
                 "Type `>servers` to see a list of servers that can be used")]
        [Alias("ps")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.ActiveMember)]
        public async Task PublicTestStartAsync(
            [Summary("The three-letter code which identifies the server to reserve.")]
            string serverCode,
            [Summary("The ID of a Steam Workshop map for the server to host.")]
            string mapId = null)
        {
            if (!_playtesting.CanReserve)
            {
                await ReplyAsync($"```Servers cannot be reserved at this time." +
                    $"\nServer reservation is blocked 1 hour before a scheduled test, and resumes once the calendar event has passed.```");
                return;
            }

            foreach (UserData u in _playtesting.UserData)
            {
                if (u.User == Context.Message.Author)
                {
                    TimeSpan timeLeft = u.ReservationExpiration.Subtract(DateTime.Now);
                    await ReplyAsync($"```You have a reservation on {u.ReservedServer.name}. You have {timeLeft:h\'H \'m\'M\'} left.```");
                    return;
                }
            }

            //Get the server
            var server = await _dataService.GetServer(serverCode);

            //Cannot find server
            if (server == null)
            {
                var authBuilder = new EmbedAuthorBuilder()
                {
                    Name = $"Hey there {Context.Message.Author.Username}!",
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                };

                var builder = new EmbedBuilder()
                {
                    Author = authBuilder,
                    ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                    Color = new Color(243, 128, 72),

                    Description = $"I could not find a server with that prefix." +
                    $"\nA server can be reserved by using `>PublicServer [serverPrefix]`. Using just `>PublicServer` will display all the servers you can use."
                };
                await ReplyAsync("", false, builder);
                return;
            }

            //Check if there is already a reservation on that server
            //If the server is open, reserve it
            if (_playtesting.IsServerOpen(server))
            {
                //Add reservation
                await _playtesting.AddServerReservation((SocketGuildUser)Context.User, DateTime.Now.AddHours(2), server);

                var authBuilder = new EmbedAuthorBuilder()
                {
                    Name = $"Hey there {Context.Message.Author} you have {server.address} for 2 hours!",
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                };
                var footBuilder = new EmbedFooterBuilder()
                {
                    Text = $"This is in beta, please let TopHATTwaffle know if you have issues.",
                    IconUrl = _client.CurrentUser.GetAvatarUrl()
                };
                List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();
                fieldBuilder.Add(new EmbedFieldBuilder { Name = "Connect Info", Value = $"`connect {server.address}`", IsInline = false });
                fieldBuilder.Add(new EmbedFieldBuilder { Name = "Links", Value = $"[Schedule a Playtest](https://www.tophattwaffle.com/playtesting/) | [View Testing Calendar](http://playtesting.tophattwaffle.com)", IsInline = false });
                var builder = new EmbedBuilder()
                {
                    Fields = fieldBuilder,
                    Footer = footBuilder,
                    Author = authBuilder,
                    ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                    Color = new Color(243, 128, 72),

                    Description = $"For the next 2 hours you can use:" +
                    $"\n`>PublicCommand [command]` or `>pc [command]`" +
                    $"\nTo send commands to the server. Example: `>pc mp_restartgame 1`" +
                    $"\nTo see a list of the commands you can use, type `>pc`" +
                    $"\nOnce the 2 hours has ended you won't have control of the server any more." +
                    $"\n\n*If you cannot connect to the reserved server for any reason, please let TopHATTwaffle know!*"
                };
                await ReplyAsync("", false, builder);

                //If they provided a map ID, change the map
                if (mapId != null)
                {
                    await Task.Delay(3000);
                    await _dataService.RconCommand($"host_workshop_map {mapId}", server);
                }
            }
            //Server is already reserved by someone else
            else
            {
                DateTime time = DateTime.Now;
                foreach (UserData u in _playtesting.UserData)
                {
                    if (u.ReservedServer == server)
                        time = u.ReservationExpiration;
                }
                TimeSpan timeLeft = time.Subtract(DateTime.Now);

                var authBuilder = new EmbedAuthorBuilder()
                {
                    Name = $"Unable to Reserver Server for {Context.Message.Author.Username}!",
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                };

                var builder = new EmbedBuilder()
                {
                    Author = authBuilder,
                    ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                    Color = new Color(243, 128, 72),

                    Description = $"You cannot reserve the server {server.name} because someone else is using it. Their reservation ends in {timeLeft:h\'H \'m\'M\'}" +
                    $"\nYou can use `>sr` to see all current server reservations."
                };
                await ReplyAsync("", false, builder);
            }
            await DataBaseUtil.AddCommandAsync("PublicServer", Context);
        }

        [Command("Servers")]
        [Summary("Lists all available servers.")]
        [Alias("list", "ListServers", "ls")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.ActiveMember, Role.Moderators, Role.RconAccess)]
        public async Task ListServersAsync()
        {
            await ReplyAsync(string.Empty, false, await _dataService.GetAllServers());

            await DataBaseUtil.AddCommandAsync("Servers", Context);
        }

        [Command("PublicCommand")]
        [Summary("Invokes a command on the invoking user's reserved test server.")]
        [Remarks(
            "One must have a server already reserved to use this command.\n\n" +
            "A reservation can be extended by 30 minutes using `>pc extend`.")]
        [Alias("pc")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.ActiveMember)]
        public async Task PublicTestCommandAsync([Remainder]string command = null)
        {
            Server server = null;

            if (!_playtesting.CanReserve)
            {
                await ReplyAsync($"```Servers cannot be reserved at this time." +
                    $"\nServer reservation is blocked 1 hour before a scheudled test, and resumes once the calendar event has passed.```");
                return;
            }

            //Display all the commands the user can use.
            if (command == null)
            {
                StringBuilder sv = new StringBuilder();
                StringBuilder mp = new StringBuilder();
                StringBuilder bot = new StringBuilder();
                StringBuilder exec = new StringBuilder();
                StringBuilder misc = new StringBuilder();

                foreach (var s in _dataService.PublicCommandWhiteList)
                {
                    if (s.StartsWith("sv"))
                    {
                        sv.AppendLine(s);
                        continue;
                    }
                    if (s.StartsWith("mp"))
                    {
                        mp.AppendLine(s);
                        continue;
                    }
                    if (s.StartsWith("bot"))
                    {
                        bot.AppendLine(s);
                        continue;
                    }
                    if (s.StartsWith("exec") || s.StartsWith("game"))
                    {
                        exec.AppendLine(s);
                        continue;
                    }

                    misc.AppendLine(s);
                }

                var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = "Allowed Commands on Test Servers",
                        IconUrl = _client.Guilds.FirstOrDefault()?.IconUrl
                    },
                    Fields =
                    {
                        new EmbedFieldBuilder
                        {
                            Name = "SV Commands",
                            Value = sv.ToString(),
                            IsInline = true
                        },
                        new EmbedFieldBuilder
                        {
                            Name = "MP Commands",
                            Value = mp.ToString(),
                            IsInline = true
                        },
                        new EmbedFieldBuilder
                        {
                            Name = "Bot Commands",
                            Value = bot.ToString(),
                            IsInline = true
                        },
                        new EmbedFieldBuilder
                        {
                            Name = "Game Mode Commands",
                            Value = exec.ToString(),
                            IsInline = true
                        },
                        new EmbedFieldBuilder
                        {
                            Name = "Other Commands",
                            Value = misc.ToString(),
                            IsInline = true
                        }
                    },
                    Color = new Color(240, 235, 230),
                };

                await ReplyAsync(string.Empty, false, embed.Build());

                return;
            }

                //Find the server that the user has reserved
                UserData user = null;
                foreach (UserData u in _playtesting.UserData)
                {
                    if (u.User == Context.Message.Author)
                    {
                        user = u;
                        server = u.ReservedServer;
                    }
                }

                //Server found, process command
                if(server != null)
                {
                    if (command.ToLower().Equals("extend"))
                    {
                        if (user.CanExtend())
                        {
                            user.ReservationExpiration = user.ReservationExpiration.AddMinutes(30);
                            TimeSpan timeLeft = user.ReservationExpiration.Subtract(DateTime.Now);
                            await ReplyAsync("```Your server reservation has been extended by 30 minutes" +
                                             $"\nNew time Left: {timeLeft:h\'H \'m\'M\'}```");
                        }
                        else
                        {
                            TimeSpan timeLeft = user.ReservationExpiration.Subtract(DateTime.Now);
                            await ReplyAsync($"```You cannot extend your reservation until there is less than 30 minutes left on the reservation." +
                                             $"\nCurrent time Left: {timeLeft:h\'H \'m\'M\'}```");
                        }
                        return;
                    }

                    if (command.Contains(";"))
                    {
                        await ReplyAsync("```You cannot use ; in a command sent to a server.```");
                        return;
                    }
                    bool valid = false;
                    if (_dataService.PublicCommandWhiteList.Any(s => command.ToLower().Contains(s))) {

                    valid = true;
                    string reply = await _dataService.RconCommand(command, server);
                    Console.WriteLine($"RCON:\n{reply}");

                    //Remove log messages from log
                    string[] replyArray = reply.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    reply = string.Join("\n", replyArray.Where(x => !x.Trim().StartsWith("L ")));
                    reply = reply.Replace("discord.gg", "discord,gg").Replace(server.rcon_password, "[PASSWORD HIDDEN]");

                    //Limit command output
                    if (reply.Length > 1880)
                        reply = $"{reply.Substring(0, 1880)}\n[OUTPUT OMITTED]";

                    //Special handling case for a password
                    if (command.Contains("sv_password"))
                    {
                        await Context.Message.DeleteAsync(); //Message was setting password, delete it.
                        await ReplyAsync($"```Command Sent to {server.name}\nA password was set on the server.```");
                    }
                    //Normal command
                    else
                        await ReplyAsync($"```{command} sent to {server.name}\n{reply}```");

                    await _dataService.ChannelLog($"{Context.User} Sent RCON command using public command", $"{command} was sent to: {server.address}\n{reply}");
                }
                //Command isn't valid
                if (!valid)
                {
                    await ReplyAsync($"```{command} cannot be sent to {server.name} because the command is not allowed.```" +
                        $"\nYou can use `>pc` to see all commands that can be sent to the server.");
                }
            }
            //No reservation found
            else
            {
                var authBuilder = new EmbedAuthorBuilder()
                {
                    Name = $"Hey there {Context.Message.Author}!",
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                };

                var builder = new EmbedBuilder()
                {
                    Author = authBuilder,
                    ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                    Color = new Color(243, 128, 72),

                    Description = $"I was unable to find a server reservation for you. You'll need to reserve a server before you can send commands." +
                    $" A server can be reserved by using `>PublicServer [serverPrefix]`. Using just `>PublicServer` will display all the servers you can use."
                };
                await ReplyAsync("", false, builder);
            }

            await DataBaseUtil.AddCommandAsync("PublicCommand", Context);
        }

        [Command("PublicAnnounce")]
        [Summary("Announces a community run playtest")]
        [Alias("pa")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.ActiveMember)]
        public async Task PublicTestAnnounceAsync(string serverCode = null)
        {
            Server server = null;

            if (!_playtesting.CanReserve)
            {
                await ReplyAsync(
                    $"```Servers cannot be reserved at this time." +
                    $"\nServer reservation is blocked 1 hour before a scheudled test, and resumes once the calendar event has passed.```");

                return;
            }

            if (serverCode == null)
            {

                //Find the server that the user has reserved
                UserData user = null;

                foreach (UserData u in _playtesting.UserData)
                {
                    if (u.User == Context.Message.Author)
                    {
                        user = u;
                        server = u.ReservedServer;
                    }
                }
            }
            else
            {
                server = await _dataService.GetServer(serverCode);
            }

            //Server found, process command
            if (server != null)
            {
                string reply = await _dataService.RconCommand("host_map", server);
                reply = reply.Substring(14, reply.IndexOf(".bsp", StringComparison.Ordinal) - 14);
                Embed wsEmbed = null;

                string[] result = reply.Split('/');

                //If larger than 1, we are a workshop map.
                if (result.Length == 3)
                {
                    reply = result[2];
                    wsEmbed = await _wsItem.HandleWorkshopEmbeds(null,null,null,result[1]);
                }

                await _dataService.TestingChannel.SendMessageAsync($"Hey {_dataService.CommunityTesterRole.Mention}!\n\n {Context.User.Mention} " +
                                                                   $"needs players to help test `{reply}`\n\nYou can join using: `connect {server.address}`",false, wsEmbed);
            }

            //No reservation found
            else
            {
                var authBuilder = new EmbedAuthorBuilder()
                {
                    Name = $"Hey there {Context.Message.Author}!",
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                };

                var builder = new EmbedBuilder()
                {
                    Author = authBuilder,
                    ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                    Color = new Color(243, 128, 72),

                    Description = $"I was unable to find a server reservation for you. You'll need to reserve a server before you can send commands." +
                    $" A server can be reserved by using `>PublicServer [serverPrefix]`. Using just `>PublicServer` will display all the servers you can use."
                };
                await ReplyAsync("", false, builder);
            }

            await DataBaseUtil.AddCommandAsync("PublicAnnounce", Context);
        }

        [Command("ReleaseServer")]
        [Summary("Releases the invoking user's reservation on a public server.")]
        [Alias("rs")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.ActiveMember)]
        public async Task ReleasePublicTestCommandAsync([Remainder]string command = null)
        {
            Server server = null;

            if (!_playtesting.CanReserve)
            {
                await ReplyAsync($"```Servers cannot be reserved at this time." +
                                 $"\nServer reservation is blocked 1 hour before a scheduled test, and resumes once the calendar event has passed.```");
                return;
            }
            bool hasServer = false;
            foreach (UserData u in _playtesting.UserData)
            {
                if (u.User != Context.Message.Author)
                    continue;
                server = u.ReservedServer;
                hasServer = true;
            }

            if (hasServer)
            {
                await ReplyAsync("```Releasing Server reservation.```");
                await _playtesting.ClearServerReservations(server.name);
            }
            else
            {
                await ReplyAsync("```I could not locate a server reservation for your account.```");
            }

            await DataBaseUtil.AddCommandAsync("ReleaseServer", Context);
        }

        [Command("ShowReservations")]
        [Summary("Displays all currently reserved servers.")]
        [Alias("sr")]
        [RequireContext(ContextType.Guild)]
        public async Task ShowReservationsAsync()
        {
            await ReplyAsync("", false, _playtesting.DisplayServerReservations());

            await DataBaseUtil.AddCommandAsync("ShowReservations", Context);
        }

        [Command("Playtester")]
        [Summary("Toggles the Playtester role for the invoking user.")]
        [Remarks("Effectively toggles one's subscription to playtesting notifications.")]
        [Alias("pt")]
        [RequireContext(ContextType.Guild)]
        public async Task PlaytesterAsync()
        {
            var user = Context.User as SocketGuildUser;

            if (user.Roles.Contains(_dataService.PlayTesterRole))
            {
                await _dataService.ChannelLog($"{Context.User} has unsubscribed from playtest notifications!");
                await ReplyAsync($"Sorry to see you go from playtest notifications {Context.User.Mention}!");
                await ((IGuildUser)user).RemoveRoleAsync(_dataService.PlayTesterRole);
            }
            else
            {
                await _dataService.ChannelLog($"{Context.User} has subscribed to playtest notifications!");
                await ReplyAsync($"Thanks for subscribing to playtest notifications {Context.User.Mention}!");
                await ((IGuildUser)user).AddRoleAsync(_dataService.PlayTesterRole);
            }

            await DataBaseUtil.AddCommandAsync("Playtester", Context);
        }
    }
}
