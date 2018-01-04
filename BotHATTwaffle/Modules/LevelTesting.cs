using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using Discord.Rest;
using System.IO;
using System.Text.RegularExpressions;
using BotHATTwaffle.Modules.Json;

namespace BotHATTwaffle.Modules
{
    public class LevelTesting
    {
        public List<UserData> userData = new List<UserData>();
        private readonly DataServices _dataServices;
        public IUserMessage  AnnounceMessage { get; set; }
        public GoogleCalendar _googleCalendar;
        public string[] lastEventInfo;
        public string[] currentEventInfo;
        Boolean alertedHour = false;
        Boolean alertedStart = false;
        Boolean alertedTwenty = false;
        Boolean alertedFifteen = false;
        public Boolean canReserve = true;
        int caltick = 0;
        string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
        string announcePath = "announcement_id.txt";
        Boolean firstRun = true;

        public LevelTesting(DataServices dataServices)
        {
            _dataServices = dataServices;


            _dataServices.calUpdateTicks = _dataServices.calUpdateTicks - 1; //This is so the if statement does not add 1.
            _googleCalendar = new GoogleCalendar(_dataServices);
            currentEventInfo = _googleCalendar.GetEvents(); //Initial get of playtest.
            lastEventInfo = currentEventInfo; //Make sure array is same size for doing compares later.
        }

        //Thanks to TimeForANinja for figuring this out!
        async private void GetPreviousAnnounceAsync()
        {
            string[] announceData = File.ReadAllLines(announcePath);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Announcement Message File Found!\n{announceData[0]}\n{announceData[1]}");

            var announceID = Convert.ToUInt64(announceData[1]);

            if (announceData[0] == currentEventInfo[2]) //If saved title == current title
            {
                Console.WriteLine("Titles match! Attempting to reattach!");
                try
                {
                    AnnounceMessage = await _dataServices.announcementChannel.GetMessageAsync(announceID) as IUserMessage;
                    Console.WriteLine("SUCCESS!");
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to load previous announcement message");
                }
            }
            else
            {
                Console.WriteLine("Titles do not match. Attempting to delete old message!");
                try
                {
                    AnnounceMessage = await _dataServices.announcementChannel.GetMessageAsync(announceID) as IUserMessage;
                    await AnnounceMessage.DeleteAsync();
                    AnnounceMessage = null;
                    Console.WriteLine("Old message Deleted!");
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Could not delete old message. Was it already deleted?");
                }
            }

            Console.ResetColor();
        }

        public async Task Announce()
        {
            //First program run and an announce file exists.
            if (firstRun && File.Exists(announcePath))
            {
                firstRun = false;
                GetPreviousAnnounceAsync();
            }

            caltick++;
            if (_dataServices.calUpdateTicks < caltick)
            {
                caltick = 0;
                currentEventInfo = _googleCalendar.GetEvents();

                if (AnnounceMessage == null) //No current message.
                {
                    await PostAnnounce(await FormatPlaytestInformationAsync(currentEventInfo, false));
                }
                else if (currentEventInfo[2] == lastEventInfo[2]) //Title is same. 
                {
                    await UpdateAnnounce(await FormatPlaytestInformationAsync(currentEventInfo, false));
                }
                else //Title is different, scrub and rebuild
                {
                    await RebuildAnnounce();
                }
            }

        }

        private async Task PostAnnounce(Embed embed)
        {
            AnnounceMessage = await _dataServices.announcementChannel.SendMessageAsync("",false,embed) as IUserMessage;

            //If the file exists, just delete it so it can be remade with the new test info.
            if (File.Exists(announcePath))
            {
                File.Delete(announcePath);
            }

                //Create the text file containing the announce message
            if (!File.Exists(announcePath))
            {
                using (StreamWriter sw = File.CreateText(announcePath))
                {
                    sw.WriteLine(currentEventInfo[2]);
                    sw.WriteLine(AnnounceMessage.Id);
                }
            }

            await _dataServices.ChannelLog("Posting Playtest Announcement", $"Posting Playtest for {currentEventInfo[2]}");
            lastEventInfo = currentEventInfo;
        }

        private async Task UpdateAnnounce(Embed embed)
        {
            try
            {
                await AnnounceMessage.ModifyAsync(x =>
                {
                    x.Content = "";
                    x.Embed = embed;
                });
                lastEventInfo = currentEventInfo;
            }
            catch
            {
                await _dataServices.ChannelLog("Attempted to modify announcement message, but I could not find it. Did someone delete it? Recreating a new message.");
                AnnounceMessage = null;
                await Announce();
            }
        }

        private async Task RebuildAnnounce()
        {
            await _dataServices.ChannelLog("Scrubbing Playtest Announcement", "Playtest is different from the last one. This is probably because" +
                "the last playtest is past. Let's tear it down and get the next test.");
            await AnnounceMessage.DeleteAsync();
            AnnounceMessage = null;
            lastEventInfo = currentEventInfo;

            //Reset announcement flags.
            alertedHour = false;
            alertedStart = false;
            alertedTwenty = false;
            alertedFifteen = false;
            canReserve = true;

            await Announce();
        }

        async public Task SetupServerAsync(string serverStr, Boolean type)
        {
            //type true = Change map
            //type false = set config
            var server = _dataServices.GetServer(serverStr.Substring(0, 3));

            if (type) //Change map
            {
                var result = Regex.Match(currentEventInfo[6], @"\d+$").Value;
                await _dataServices.RconCommand($"host_workshop_map {result}", server);
                await _dataServices.ChannelLog("Changing Map on Test Server", $"'host_workshop_map {result}' on {server.Address}");
            }
            else //Set config and post about it
            {
                
                var builder = new EmbedBuilder();
                var authBuilder = new EmbedAuthorBuilder();
                List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();
                authBuilder = new EmbedAuthorBuilder()
                {
                    Name = $"Setting up {server.Address} for {currentEventInfo[2]}",
                    IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
                };

                fieldBuilder.Add(new EmbedFieldBuilder { Name = "Connect Info", Value = $"`connect {currentEventInfo[10]}`", IsInline = false });

                builder = new EmbedBuilder()
                {
                    Author = authBuilder,
                    Fields = fieldBuilder,
                    Title = $"Workshop Link",
                    Url = currentEventInfo[6],
                    ThumbnailUrl = currentEventInfo[4],
                    Color = new Color(71, 126, 159),

                    Description = $"**{server.Description}**\n\n{currentEventInfo[9]}"
                };
                await _dataServices.testingChannel.SendMessageAsync("", false, builder);
                await _dataServices.ChannelLog("Setting postgame config", $"'exec postgame' on {server.Address}");
                await _dataServices.RconCommand($"exec postgame", server);
            }
        }

        async public Task<Embed> FormatPlaytestInformationAsync(string[] eventInfo, Boolean userCall)
        {
            //0 EVENT HEADER. "BEGIN_EVENT" or "NO_EVENT_FOUND"
            //1 Time-
            //2 Title-
            //3 Creator-
            //4 Featured Image-
            //5 Map Images-
            //6 Workshop Link-
            //7 Game Mode-
            //8 Moderator-
            //9 Description-
            //10 Location-

            var builder = new EmbedBuilder();
            var authBuilder = new EmbedAuthorBuilder();
            List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();
            var footBuilder = new EmbedFooterBuilder();

            if (eventInfo[0].Equals("BEGIN_EVENT"))
            {
                DateTime time = Convert.ToDateTime(eventInfo[1]);
                string timeStr = time.ToString("MMMM ddd d, HH:mm");
                TimeSpan timeLeft = time.Subtract(DateTime.Now);
                string timeLeftStr = null;
                DateTime utcTime = time.ToUniversalTime();

                //Timezones!
                string est = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")).ToString("ddd HH:mm");
                string pst = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time")).ToString("ddd HH:mm");
                string gmt = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")).ToString("ddd HH:mm");
                //Screw Australia. 
                //string gmt8 = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("W. Australia Standard Time")).ToString("ddd HH:mm");

                string postTime = $"{timeStr} CT | {est} EST | {pst} PST | {gmt} GMT"; // | {gmt8} GMT+8";

                //Check if we need to adjust the time until for after a test starts.
                if (time.CompareTo(DateTime.Now) < 0)
                {
                    timeLeftStr = $"Started: {timeLeft.ToString("h'H 'm'M'")} ago!";
                    if(!userCall && !alertedStart) //Prevents user calls for upcoming from sending alert message.
                    {
                        alertedStart = true;
                        await  _dataServices.playTesterRole.ModifyAsync(x =>
                        {
                            x.Mentionable = true;
                        });

                        //Display the map to be tested.
                        await _dataServices.testingChannel.SendMessageAsync("", false, await FormatPlaytestInformationAsync(currentEventInfo, true));
                        await _dataServices.testingChannel.SendMessageAsync($"{_dataServices.playTesterRole.Mention}" +
                        $"\n**Playtest starting now!** `connect {eventInfo[10]}`" +
                        $"\n*Type `>playtester` to unsubscribe*");

                        alertedStart = true;

                        await _dataServices.playTesterRole.ModifyAsync(x =>
                        {
                            x.Mentionable = false;
                        });
                    }
                }
                else
                {
                    timeLeftStr = timeLeft.ToString("d'D 'h'H 'm'M'").TrimStart(' ', 'D', 'H', '0');
                }

                //Let's check if we should be announcing a playtest. Easier to do it here since the variables are already computed.
                TimeSpan singleHour = new TimeSpan(1, 0, 0);
                DateTime adjusted = DateTime.Now.Add(singleHour);
                int timeCompare = DateTime.Compare(adjusted, time);
                if (timeCompare > 0 && !alertedHour)
                {
                    canReserve = false;
                    await ClearServerReservations();

                    alertedHour = true;
                    await _dataServices.playTesterRole.ModifyAsync(x =>
                    {
                        x.Mentionable = true;
                    });

                    //Display the map to be tested.
                    await _dataServices.testingChannel.SendMessageAsync("", false, await FormatPlaytestInformationAsync(currentEventInfo, true));

                    await _dataServices.testingChannel.SendMessageAsync($"{_dataServices.playTesterRole.Mention}" +
                            $"\n**Playtest starting in 1 hour**" +
                            $"\n*Type `>playtester` to unsubscribe*");

                    await _dataServices.playTesterRole.ModifyAsync(x =>
                    {
                        x.Mentionable = false;
                    });
                }

                //Change map 20 minutes beforehand
                TimeSpan twentyMinutes = new TimeSpan(0, 20, 0);
                DateTime twentyAdjusted = DateTime.Now.Add(twentyMinutes);
                int twentyTimeCompare = DateTime.Compare(twentyAdjusted, time);
                if (twentyTimeCompare > 0 && !alertedTwenty)
                {
                    alertedTwenty = true;
                    await SetupServerAsync(eventInfo[10], true);
                }

                //Exec postgame config for people to mess around on the server
                TimeSpan fifteenMinutes = new TimeSpan(0, 15, 0);
                DateTime fifteenAdjusted = DateTime.Now.Add(fifteenMinutes);
                int fifteenTimeCompare = DateTime.Compare(fifteenAdjusted, time);
                if (fifteenTimeCompare > 0 && !alertedFifteen)
                {
                    alertedFifteen = true;
                    await SetupServerAsync(eventInfo[10], false);
                }


                authBuilder = new EmbedAuthorBuilder()
                {
                    Name = eventInfo[2],
                    IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
                };


                fieldBuilder.Add(new EmbedFieldBuilder { Name = "Time Until Test", Value = timeLeftStr, IsInline = true });
                fieldBuilder.Add(new EmbedFieldBuilder { Name = "Where?", Value = $"`{eventInfo[10]}`", IsInline = true });
                fieldBuilder.Add(new EmbedFieldBuilder { Name = "Creator", Value = eventInfo[3], IsInline = true });
                fieldBuilder.Add(new EmbedFieldBuilder { Name = "Moderator", Value = eventInfo[8], IsInline = true });
                fieldBuilder.Add(new EmbedFieldBuilder { Name = "Links", Value = $"[Map Images]({eventInfo[5]}) | [Schedule a Playtest](https://www.tophattwaffle.com/playtesting/) | [View Testing Calendar](http://playtesting.tophattwaffle.com)", IsInline = false });
                fieldBuilder.Add(new EmbedFieldBuilder { Name = "When?", Value = postTime, IsInline = false });

                footBuilder = new EmbedFooterBuilder()
                {
                    Text = $"connect {eventInfo[10]}",
                    IconUrl = Program._client.CurrentUser.GetAvatarUrl()
                };

                builder = new EmbedBuilder()
                {
                    Author = authBuilder,
                    Footer = footBuilder,
                    Fields = fieldBuilder,

                    Title = $"Workshop Link",
                    Url = eventInfo[6],
                    ImageUrl = eventInfo[4],
                    ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                    Color = new Color(71, 126, 159),

                    Description = $"{eventInfo[9]}\n*level is loaded on the server 15 minutes before the start time.*"
                };
            }
            else
            {
                string announceDiag = null;
                if (eventInfo[0].Equals("BAD_DESCRIPTION"))
                    announceDiag = "\n\n\nThere was an issue with the Google Calendar event. Someone tell TopHATTwaffle..." +
                        "If you're seeing this, that means there is probably a test scheduled, but the description contains " +
                        "HTML code so I cannot properly parse it. ReeeeeeEEEeeE";

                //_dataServices.ChannelLog($"No playtest was found. Posting default message.");
                authBuilder = new EmbedAuthorBuilder()
                {
                    Name = "No Playtests Found!",
                    IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
                };

                footBuilder = new EmbedFooterBuilder()
                {
                    Text = "https://www.tophattwaffle.com/playtesting/",
                    IconUrl = Program._client.CurrentUser.GetAvatarUrl()
                };

                builder = new EmbedBuilder()
                {
                    Author = authBuilder,
                    Footer = footBuilder,

                    Title = $"Click here to schedule your playtest!",
                    Url = "https://www.tophattwaffle.com/playtesting/",
                    ImageUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/header.png",
                    //ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                    Color = new Color(214, 91, 47),

                    Description = $"Believe it or not, there aren't any tests scheduled. Click the link above to schedule your own playtest! {announceDiag}"
                };
            }
            return builder.Build();
        }

        public async Task CheckServerReservations()
        {
            //Loop reservations and clear them if needed.
            foreach (UserData u in userData.ToList())
            {
                if (u.CanReleaseServer())
                {
                    var authBuilder = new EmbedAuthorBuilder()
                    {
                        Name = $"Hey there {u.User.Username}!",
                        IconUrl = u.User.GetAvatarUrl(),
                    };
                    var footBuilder = new EmbedFooterBuilder()
                    {
                        Text = $"This is in beta, please let TopHATTwaffle know if you have issues.",
                        IconUrl = Program._client.CurrentUser.GetAvatarUrl()
                    };

                    var builder = new EmbedBuilder()
                    {
                        Footer = footBuilder,
                        Author = authBuilder,
                        ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                        Color = new Color(243, 128, 72),

                        Description = $"Your reservation on {u.Server.Description} has ended! You can stay on the server but you cannot send any more commands to it."
                    };

                    try //If we cannot send a DM to the user, just dump it into the testing channel and tag them.
                    {
                        await u.User.SendMessageAsync("", false, builder);
                    }
                    catch
                    {
                        await _dataServices.testingChannel.SendMessageAsync(u.User.Mention, false, builder);
                    }

                    await _dataServices.ChannelLog($"{u.User}'s reservation on {u.Server.Address} has ended.");
                    await _dataServices.RconCommand($"sv_cheats 0;sv_password \"\";say Hey there {u.User.Username}! Your reservation on this server has ended!", u.Server);
                    userData.Remove(u);
                    await Task.Delay(1000);
                }
            }
        }

        async public Task AddServerReservation(SocketGuildUser inUser, DateTime inServerReleaseTime, Json.JsonServer server)
        {
            await _dataServices.ChannelLog($"{inUser} reservation on {server.Address} has started.", $"Reservation expires at {inServerReleaseTime}");
            await _dataServices.RconCommand($"say Hey everyone! {inUser.Username} has reserved this server!", server);
            userData.Add(new UserData()
            {
                User = inUser,
                Server = server,
                ServerReleaseTime = inServerReleaseTime
            });
        }

        //Clears all reservations
        public async Task ClearServerReservations()
        {
            foreach (UserData u in userData.ToList())
            {
                var authBuilder = new EmbedAuthorBuilder()
                {
                    Name = $"Hey there {u.User.Username}!",
                    IconUrl = u.User.GetAvatarUrl(),
                };
                var footBuilder = new EmbedFooterBuilder()
                {
                    Text = $"This is in beta, please let TopHATTwaffle know if you have issues.",
                    IconUrl = Program._client.CurrentUser.GetAvatarUrl()
                };

                var builder = new EmbedBuilder()
                {
                    Footer = footBuilder,
                    Author = authBuilder,
                    ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                    Color = new Color(243, 128, 72),

                    Description = $"Your reservation on server {u.Server.Description} has expired because all reservations were cleared." +
                    $"This is likely due to a playtest starting soon, or a moderator cleared all reservations."
                };

                try
                {
                    await u.User.SendMessageAsync("", false, builder);
                }
                catch
                {
                    await _dataServices.testingChannel.SendMessageAsync(u.User.Mention, false, builder);
                }

                await _dataServices.ChannelLog($"{u.User}'s reservation on {u.Server.Address} has ended.");
                await _dataServices.RconCommand($"sv_cheats 0;sv_password \"\"", u.Server);
                userData.Remove(u);
                await Task.Delay(1000);
            }
        }

        //Clears a specific reservation    
        public async Task ClearServerReservations(string serverStr)
        {
            var server = _dataServices.GetServer(serverStr);

            if (server == null)
                return;

            foreach (UserData u in userData.ToList())
            {
                if (u.Server == server)
                {
                    var authBuilder = new EmbedAuthorBuilder()
                    {
                        Name = $"Hey there {u.User.Username}!",
                        IconUrl = u.User.GetAvatarUrl(),
                    };
                    var footBuilder = new EmbedFooterBuilder()
                    {
                        Text = $"This is in beta, please let TopHATTwaffle know if you have issues.",
                        IconUrl = Program._client.CurrentUser.GetAvatarUrl()
                    };

                    var builder = new EmbedBuilder()
                    {
                        Footer = footBuilder,
                        Author = authBuilder,
                        ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
                        Color = new Color(243, 128, 72),

                        Description = $"Your reservation on server {u.Server.Description} has expired because the reservation was cleared." +
                        $"This is likely due to a playtest starting soon, a moderator cleared the reservation, or you released the reservation."
                    };

                    try
                    {
                        await u.User.SendMessageAsync("", false, builder);
                    }
                    catch
                    {
                        await _dataServices.testingChannel.SendMessageAsync(u.User.Mention, false, builder);
                    }

                    await _dataServices.ChannelLog($"{u.User}'s reservation on {u.Server.Address} has ended.");
                    await _dataServices.RconCommand($"sv_cheats 0;sv_password \"\"", u.Server);
                    userData.Remove(u);
                    await Task.Delay(1000);
                }
            }
        }

        public Embed DisplayServerReservations()
        {
            //Loop reservations and clear them if needed.
            var authBuilder = new EmbedAuthorBuilder()
            {
                Name = $"Current Server Reservations",
                //IconUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
            };

            List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();

            foreach (UserData u in userData.ToList())
            {
                TimeSpan timeLeft = u.ServerReleaseTime.Subtract(DateTime.Now);
                fieldBuilder.Add(new EmbedFieldBuilder { Name = $"{u.Server.Address}", Value = $"User: `{u.User}#{u.User.Discriminator}`\nTime Left: {timeLeft.ToString("h'H 'm'M'")}", IsInline = false });
            }

            string description = null;

            if (fieldBuilder.Count == 0)
                description = "No reservations found";

            var builder = new EmbedBuilder()
            {
                Fields = fieldBuilder,
                Author = authBuilder,
                Color = new Color(243, 128, 72),
                ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",

                Description = description
            };

            return builder;
        }

        public Boolean IsServerOpen(JsonServer server)
        {
            foreach (UserData u in userData.ToList())
            {
                if (u.Server == server)
                    return false;
            }
            return true;
        }
    }

    public class LevelTestingModule : ModuleBase<SocketCommandContext>
    {
        private readonly LevelTesting _levelTesting;
        private readonly DataServices _dataServices;
        

        public LevelTestingModule(LevelTesting levelTesting, DataServices dataServices)
        {
            _levelTesting = levelTesting;
            _dataServices = dataServices;
        }

        [Command("PublicServer")]
        [Summary("`>PublicServer [serverPrefix]` Reserves a public server for your own testing use.")]
        [Remarks("`>ps eus` Reserves a server for 2 hours for you to use for testing purposes." +
            "\nYou can also include a Workshop ID to load that map automatically. `>ps eus 123456789`." +
            "\nTo see a list of servers use `>ps`")]
        [Alias("ps")]
        public async Task PublicTestStartAsync(string serverStr = null, string mapID = null)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("***This command can not be used in a DM***");
                return;
            }

            if ((Context.User as SocketGuildUser).Roles.Contains(_dataServices.ActiveRole))
            {
                if (!_levelTesting.canReserve)
                {
                    await ReplyAsync($"```Servers cannot be reserved at this time." +
                        $"\nServer reservation is blocked 1 hour before a scheduled test, and resumes once the calendar event has passed.```");
                    return;
                }

                foreach (UserData u in _levelTesting.userData)
                {
                    if (u.User == Context.Message.Author)
                    {
                        TimeSpan timeLeft = u.ServerReleaseTime.Subtract(DateTime.Now);
                        await ReplyAsync($"```You have a reservation on {u.Server.Name}. You have {timeLeft.ToString("h'H 'm'M'")} left.```");
                        return;
                    }
                }

                //Display list of servers
                if (serverStr == null && mapID == null)
                {
                    await ReplyAsync("",false,_dataServices.GetAllServers());
                    return;
                }

                var server = _dataServices.GetServer(serverStr);

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
                if (_levelTesting.IsServerOpen(server))
                {
                    await _levelTesting.AddServerReservation((SocketGuildUser)Context.User, DateTime.Now.AddHours(2), server);

                    var authBuilder = new EmbedAuthorBuilder()
                    {
                        Name = $"Hey there {Context.Message.Author} you have {server.Address} for 2 hours!",
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                    };
                    var footBuilder = new EmbedFooterBuilder()
                    {
                        Text = $"This is in beta, please let TopHATTwaffle know if you have issues.",
                        IconUrl = Program._client.CurrentUser.GetAvatarUrl()
                    };
                    List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();
                    fieldBuilder.Add(new EmbedFieldBuilder { Name = "Connect Info", Value = $"`connect {server.Address}`", IsInline = false });
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

                    if (mapID != null)
                    {
                        await Task.Delay(3000);
                        await _dataServices.RconCommand($"host_workshop_map {mapID}", server);
                    }
                }
                else
                {
                    DateTime time = DateTime.Now;
                    foreach (UserData u in _levelTesting.userData)
                    {
                        if (u.Server == server)
                            time = u.ServerReleaseTime;
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

                        Description = $"You cannot reserve the server {server.Name} because someone else is using it. Their reservation ends in {timeLeft.ToString("h'H 'm'M'")}" +
                        $"\nYou can use `>sr` to see all current server reservations."
                    };
                    await ReplyAsync("", false, builder);
                }
            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} is trying to use public playtest commands without permission.");
                await ReplyAsync($"```You cannot use this command with your current permission level! You need {_dataServices.ActiveRole.Name} role.```");
            }
        }

        [Command("PublicCommand")]
        [Summary("`>PublicCommand [command]` Sends command to your reserved test server")]
        [Remarks("`>pc [command]` Sends a command to your reserved server." +
            "\nExample: `>pc sv_cheats 1`" +
            "\nYou must have a server already reserved to use this command." +
            "\nUse `pc` to see all commands you can use.")]
        [Alias("pc")]
        public async Task PublicTestCommandAsync([Remainder]string command = null)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("***This command can not be used in a DM***");
                return;
            }

            if ((Context.User as SocketGuildUser).Roles.Contains(_dataServices.ActiveRole))
            {
                JsonServer server = null;

                if (!_levelTesting.canReserve)
                {
                    await ReplyAsync($"```Servers cannot be reserved at this time." +
                        $"\nServer reservation is blocked 1 hour before a scheudled test, and resumes once the calendar event has passed.```");
                    return;
                }

                if (command == null)
                {
                    string reply = null;
                    foreach (string s in _dataServices.publicCommandWhiteList)
                    {
                        reply += $"{s}, ";
                    }
                    await ReplyAsync($"__**Commands that can be used on public test servers**__" +
                                    $"```{reply}```");
                    return;
                }

                foreach (UserData u in _levelTesting.userData)
                {
                    if (u.User == Context.Message.Author)
                    {
                        server = u.Server;
                    }
                }

                if(server != null)
                {
                    if (command.Contains(";"))
                    {
                        await ReplyAsync("```You cannot use ; in a command sent to a server.```");
                        return;
                    }
                    Boolean valid = false;
                    foreach (string s in _dataServices.publicCommandWhiteList)
                    {
                        
                        if (command.ToLower().Contains(s))
                        {
                            valid = true;
                            string reply = await _dataServices.RconCommand(command, server);
                            Console.WriteLine($"RCON:\n{reply}");

                            if (reply.Length > 1880)
                                reply = $"{reply.Substring(0, 1880)}\n[OUTPUT OMITTED...]";

                            //Remove log messages from log
                            string[] replyArray = reply.Split(
                            new[] { "\r\n", "\r", "\n" },
                            StringSplitOptions.None
                            );
                            reply = string.Join("\n", replyArray.Where(x => !x.Trim().StartsWith("L ")));
                            reply = reply.Replace("discord.gg", "discord,gg").Replace(server.Password, "[PASSWORD HIDDEN]");

                            if (command.Contains("sv_password"))
                            {
                                await Context.Message.DeleteAsync(); //Message was setting password, delete it.
                                await ReplyAsync($"```Command Sent to {server.Name}\nA password was set on the server.```");
                            }
                            else
                            {
                                await ReplyAsync($"```{command} sent to {server.Name}\n{reply}```");
                            }

                            await _dataServices.ChannelLog($"{Context.User} Sent RCON command using public command", $"{command} was sent to: {server.Address}\n{reply}");
                            break;
                        }
                    }
                    if (!valid)
                    {
                        await ReplyAsync($"```{command} cannot be sent to {server.Name} because the command is not allowed.```" +
                            $"\nYou can use `>pc` to see all commands that can be sent to the server.");
                    }
                }
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
            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} is trying to use public playtest commands without permission.");
                await ReplyAsync($"```You cannot use this command with your current permission level! You need {_dataServices.ActiveRole.Name} role.```");
            }
        }

        [Command("ReleaseServer")]
        [Summary("`>ReleaseServer` Releases your reservation on the public server.")]
        [Remarks("`>ReleaseServer` or `>rs` releases the reservation you have on a server.")]
        [Alias("rs")]
        public async Task ReleasePublicTestCommandAsync([Remainder]string command = null)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("***This command can not be used in a DM***");
                return;
            }

            if ((Context.User as SocketGuildUser).Roles.Contains(_dataServices.ActiveRole))
            {
                JsonServer server = null;

                if (!_levelTesting.canReserve)
                {
                    await ReplyAsync($"```Servers cannot be reserved at this time." +
                        $"\nServer reservation is blocked 1 hour before a scheudled test, and resumes once the calendar event has passed.```");
                    return;
                }
                Boolean hasServer = false;
                foreach (UserData u in _levelTesting.userData)
                {
                    if (u.User == Context.Message.Author)
                    {
                        server = u.Server;
                        hasServer = true;
                    }
                }

                if (hasServer)
                {
                    await _levelTesting.ClearServerReservations(server.Name);
                }
                else
                {
                    await ReplyAsync("```I could not locate a server reservation for your account.```");
                }
            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} is trying to use public playtest commands without permission.");
                await ReplyAsync($"```You cannot use this command with your current permission level! You need {_dataServices.ActiveRole.Name} role.```");
            }
        }

        [Command("ShowReservations")]
        [Summary("`>sr` Shows all server reservations")]
        [Remarks("Shows all current server reservations.")]
        [Alias("sr")]
        public async Task ShowReservationsAsync(string serverStr = null)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("***This command can not be used in a DM***");
                return;
            }

            await ReplyAsync("", false, _levelTesting.DisplayServerReservations());

        }

        [Command("playtester")]
        [Summary("`>playtester` Toggles your playtest notifications.")]
        [Remarks("Toggles your subscription to the playtester notification group.")]
        [Alias("pt")]
        public async Task PlaytesterAsync()
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("**This command can not be used in a DM**");
                return;
            }
            var user = Context.User as SocketGuildUser;

            if (user.Roles.Contains(_dataServices.playTesterRole))
            {
                await _dataServices.ChannelLog($"{Context.User} has unsubscribed from playtest notifications!");
                await ReplyAsync($"Sorry to see you go from playtest notifications {Context.User.Mention}!");
                await (user as IGuildUser).RemoveRoleAsync(_dataServices.playTesterRole);
            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} has subscribed to playtest notifications!");
                await ReplyAsync($"Thanks for subscribing to playtest notifications {Context.User.Mention}!");
                await (user as IGuildUser).AddRoleAsync(_dataServices.playTesterRole);
            }
        }

        [Command("upcoming")]
        [Summary("`>upcoming` Shows you the next playtest")]
        [Remarks("Automatically looks up the next playtest for you. You can always just look in the announcement channel")]
        [Alias("up")]
        public async Task UpcomingAsync()
        {
            //Purges last and current stored info about the test. This is a easy way to reset the stored info manually
            //if something happens and the announcement glitches out.
            _levelTesting.currentEventInfo = _levelTesting._googleCalendar.GetEvents();
            _levelTesting.lastEventInfo = _levelTesting.currentEventInfo;

            await ReplyAsync("", false, await _levelTesting.FormatPlaytestInformationAsync(_levelTesting.currentEventInfo, true));
        }
    }
}
