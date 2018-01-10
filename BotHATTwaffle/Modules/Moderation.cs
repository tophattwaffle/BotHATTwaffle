using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using BotHATTwaffle.Modules.Json;
using System.Text.RegularExpressions;
using BotHATTwaffle.Objects.Downloader;
using Discord.Addons.Interactive;

namespace BotHATTwaffle.Modules
{
    public class ModerationServices
    {
        public List<UserData> muteList = new List<UserData>();
        public string[] TestInfo { get; set; }
        private readonly DataServices _dataServices;

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

    public class ModerationModule : InteractiveBase
    {
        private readonly ModerationServices _mod;
        private readonly LevelTesting _levelTesting;
        private readonly DataServices _dataServices;
        private readonly TimerService _timer;
        private readonly DownloaderService _downloaderService;

        public ModerationModule(ModerationServices mod,
                                LevelTesting levelTesting,
                                DataServices dataServices,
                                TimerService timer,
                                DownloaderService dlService)
        {
            _timer = timer;
            _dataServices = dataServices;
            _levelTesting = levelTesting;
            _mod = mod;
            _downloaderService = dlService;
        }

        [Command("announce", RunMode = RunMode.Async)]
        [Summary("`>announce` Interactively create an embed message to be sent to any channel")]
        [Remarks("You can also just dump and entire embed in one command using the following template:" +
                 "\n{Author Name}myAuthName{Thumbnail}http://www.myThumb.com{Title}myTitle{URL}http://www.myURL.com{Color}255 100 50{Description}myDesc{Image}http://www.myImg.com{Footer Text}myFooter{Field}myFieldtitle{}myFieldText{}(t|f){submit}general" +
                 "\n`{Author Name}{Thumbnail}{Title}{URL}{Color}{Description}{Image}{Footer Text}{Field}{}{}{Submit}`" +
                 "\nFields can be omitted if you do not want them. You can add multiple fields at a time if you want.")]
        [Alias("a")]
        public async Task Test_NextMessageAsync([Remainder] string inValue = null)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("```This command can not be used in a DM```");
                return;
            }
            if ((Context.User as SocketGuildUser).Roles.Contains(_dataServices.ModRole))
            {
                await Context.Message.DeleteAsync();

                var embedLayout = new EmbedBuilder()
                {
                    ImageUrl = "https://content.tophattwaffle.com/BotHATTwaffle/embed.png",
                };

                string quickSendChannel = null;

                string embedDescription = null;
                Color embedColor = new Color(243, 128, 72);
                string embedThumbUrl = null;
                string embedTitle = null;
                string embedURL = null;
                string footText = null;
                string authName = null;
                string footIconURL = null;
                string embedImageURL = null;

                List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();

                if (inValue != null)
                {
                    Regex regex = new Regex("{([^}]*)}", RegexOptions.IgnoreCase);
                    if (isValidTag(inValue, regex))
                    {
                        string errors = null;
                        while (inValue.Length > 0)
                        {
                            if (inValue.ToLower().StartsWith("{author name}"))
                            {
                                inValue = inValue.Substring(13);
                                Match m = regex.Match(inValue);
                                int textLength;
                                if (m.ToString() != "")
                                    textLength = inValue.IndexOf(m.ToString());
                                else
                                    textLength = inValue.Length;

                                authName = inValue.Substring(0, textLength);
                                inValue = inValue.Substring(textLength);
                            }

                            if (inValue.ToLower().StartsWith("{thumbnail}"))
                            {
                                inValue = inValue.Substring(11);
                                Match m = regex.Match(inValue);
                                int textLength;
                                if (m.ToString() != "")
                                    textLength = inValue.IndexOf(m.ToString());
                                else
                                    textLength = inValue.Length;

                                embedThumbUrl = inValue.Substring(0, textLength);
                                if (!Uri.IsWellFormedUriString(embedThumbUrl, UriKind.Absolute))
                                {
                                    embedThumbUrl = null;
                                    errors += "THUMBNAIL URL NOT A PROPER URL. SET TO NULL\n";
                                }

                                inValue = inValue.Substring(textLength);
                            }

                            if (inValue.ToLower().StartsWith("{title}"))
                            {
                                inValue = inValue.Substring(7);
                                Match m = regex.Match(inValue);
                                int textLength;
                                if (m.ToString() != "")
                                    textLength = inValue.IndexOf(m.ToString());
                                else
                                    textLength = inValue.Length;

                                embedTitle = inValue.Substring(0, textLength);
                                inValue = inValue.Substring(textLength);
                            }

                            if (inValue.ToLower().StartsWith("{url}"))
                            {
                                inValue = inValue.Substring(5);
                                Match m = regex.Match(inValue);
                                int textLength;
                                if (m.ToString() != "")
                                    textLength = inValue.IndexOf(m.ToString());
                                else
                                    textLength = inValue.Length;

                                embedURL = inValue.Substring(0, textLength);
                                if (!Uri.IsWellFormedUriString(embedThumbUrl, UriKind.Absolute))
                                {
                                    embedURL = null;
                                    errors += "TITLE URL NOT A PROPER URL. SET TO NULL\n";
                                }
;
                                inValue = inValue.Substring(textLength);
                            }

                            if (inValue.ToLower().StartsWith("{color}"))
                            {
                                inValue = inValue.Substring(7);
                                Match m = regex.Match(inValue);
                                int textLength;
                                if (m.ToString() != "")
                                    textLength = inValue.IndexOf(m.ToString());
                                else
                                    textLength = inValue.Length;

                                string[] splitString = {null, null, null};
                                splitString = inValue.Substring(0, textLength).Split(' ');
                                try
                                {
                                    var splitInts = splitString.Select(item => int.Parse(item)).ToArray();
                                    embedColor = new Color(splitInts[0], splitInts[1], splitInts[2]);
                                }
                                catch
                                {
                                    errors += "INVALID RGB STRUCTURE. DEFUALT COLOR USED\n";
                                }

                                inValue = inValue.Substring(textLength);
                            }

                            if (inValue.ToLower().StartsWith("{description}"))
                            {
                                inValue = inValue.Substring(13);
                                Match m = regex.Match(inValue);
                                int textLength;
                                if (m.ToString() != "")
                                    textLength = inValue.IndexOf(m.ToString());
                                else
                                    textLength = inValue.Length;

                                embedDescription = inValue.Substring(0, textLength);
                                inValue = inValue.Substring(textLength);
                            }

                            if (inValue.ToLower().StartsWith("{image}"))
                            {
                                inValue = inValue.Substring(7);
                                Match m = regex.Match(inValue);
                                int textLength;
                                if (m.ToString() != "")
                                    textLength = inValue.IndexOf(m.ToString());
                                else
                                    textLength = inValue.Length;

                                embedImageURL = inValue.Substring(0, textLength);
                                if (!Uri.IsWellFormedUriString(embedThumbUrl, UriKind.Absolute))
                                {
                                    embedImageURL = null;
                                    errors += "IMAGE URL NOT A PROPER URL. SET TO NULL\n";
                                }
                                inValue = inValue.Substring(textLength);
                            }

                            if (inValue.ToLower().StartsWith("{footer text}"))
                            {
                                inValue = inValue.Substring(13);
                                Match m = regex.Match(inValue);
                                int textLength;
                                if (m.ToString() != "")
                                    textLength = inValue.IndexOf(m.ToString());
                                else
                                    textLength = inValue.Length;

                                footText = inValue.Substring(0, textLength);
                                inValue = inValue.Substring(textLength);
                            }

                            if (inValue.ToLower().StartsWith("{field}"))
                            {
                                inValue = inValue.Substring(7);
                                Match m = regex.Match(inValue);
                                int textLength;
                                if (m.ToString() != "")
                                    textLength = inValue.IndexOf(m.ToString());
                                else
                                    textLength = inValue.Length;

                                string fieldTi = inValue.Substring(0, textLength);

                                inValue = inValue.Substring(textLength + 2);

                                //Match field text
                                m = regex.Match(inValue);
                                textLength = inValue.IndexOf(m.ToString());
                                string fieldCo = inValue.Substring(0, textLength);

                                inValue = inValue.Substring(textLength + 2);

                                //Match field inline
                                m = regex.Match(inValue);

                                if (m.ToString() != "")
                                    textLength = inValue.IndexOf(m.ToString());
                                else
                                    textLength = inValue.Length;

                                string tfStr = inValue.Substring(0, textLength);

                                bool fieldIn = tfStr.ToLower().StartsWith("t");

                                inValue = inValue.Substring(textLength);

                                fieldBuilder.Add(new EmbedFieldBuilder { Name = fieldTi, Value = fieldCo, IsInline = fieldIn });
                            }

                            if (inValue.ToLower().StartsWith("{submit}"))
                            {
                                inValue = inValue.Substring(8);
                                Match m = regex.Match(inValue);
                                int textLength;
                                if (m.ToString() != "")
                                    textLength = inValue.IndexOf(m.ToString());
                                else
                                    textLength = inValue.Length;

                                quickSendChannel = inValue.Substring(0, textLength);
                                inValue = inValue.Substring(textLength);
                            }
                        }
                        if (errors != null)
                            await ReplyAndDeleteAsync($"```The following errors occurred:\n{errors}```", timeout: TimeSpan.FromSeconds(15));
                    }
                }

                var authBuilder = new EmbedAuthorBuilder()
                {
                    Name = authName,
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                };
                var footBuilder = new EmbedFooterBuilder()
                {
                    Text = footText,
                    IconUrl = footIconURL
                };
                var builder = new EmbedBuilder()
                {
                    Fields = fieldBuilder,
                    Footer = footBuilder,
                    Author = authBuilder,

                    ImageUrl = embedImageURL,
                    Url = embedURL,
                    Title = embedTitle,
                    ThumbnailUrl = embedThumbUrl,
                    Color = embedColor,
                    Description = embedDescription

                };
                Boolean submit = false;
                if (quickSendChannel == null)
                {
                    string instructionsStr = "Type one of the options. Do not include `>`. Auto timeout in 120 seconds:" +
                                             "\n`Author Name` `Thumbnail` `Title` `URL` `Color` `Description` `Image` `Footer Text` `Field`" +
                                             "\n`submit` to send it." + "\n`cancel` to abort.";
                    var pic = await ReplyAsync("", false, embedLayout);
                    var preview = await ReplyAsync("__**PREVIEW**__", false, builder);
                    var instructions = await ReplyAsync(instructionsStr);
                    Boolean run = true;
                    while (run)
                    {
                        var response = await NextMessageAsync();
                        if (response != null)
                        {
                            try
                            {
                                await response.DeleteAsync();
                            }
                            catch
                            {
                            }

                            Boolean valid = true;
                            switch (response.Content.ToLower())
                            {
                                case "author name":
                                    await instructions.ModifyAsync(x => { x.Content = "Enter Author Name text:"; });
                                    response = await NextMessageAsync();
                                    if (response != null)
                                    {
                                        authName = response.Content;
                                    }

                                    break;

                                case "thumbnail":
                                    await instructions.ModifyAsync(x => { x.Content = "Enter Thumbnail URL:"; });
                                    response = await NextMessageAsync();
                                    if (response != null)
                                    {
                                        if (Uri.IsWellFormedUriString(response.Content, UriKind.Absolute))
                                        {
                                            embedThumbUrl = response.Content;
                                        }
                                        else
                                        {
                                            await ReplyAndDeleteAsync("```INVALID URL!```", timeout: TimeSpan.FromSeconds(3));
                                        }
                                    }

                                    break;

                                case "title":
                                    await instructions.ModifyAsync(x => { x.Content = "Enter Title text:"; });
                                    response = await NextMessageAsync();
                                    if (response != null)
                                    {
                                        embedTitle = response.Content;
                                    }

                                    break;

                                case "url":
                                    await instructions.ModifyAsync(x => { x.Content = "Enter Title URL:"; });
                                    response = await NextMessageAsync();
                                    if (response != null)
                                    {
                                        if (Uri.IsWellFormedUriString(response.Content, UriKind.Absolute))
                                        {
                                            embedURL = response.Content;
                                        }
                                        else
                                        {
                                            await ReplyAndDeleteAsync("```INVALID URL!```", timeout: TimeSpan.FromSeconds(3));
                                        }
                                    }

                                    break;

                                case "color":
                                    await instructions.ModifyAsync(x =>
                                    {
                                        x.Content = "Enter Color in form of `R G B` Example: `250 120 50` :";
                                    });
                                    response = await NextMessageAsync();
                                    string[] splitString = {null, null, null};
                                    splitString = response.Content.Split(' ');
                                    try
                                    {
                                        var splitInts = splitString.Select(item => int.Parse(item)).ToArray();
                                        embedColor = new Color(splitInts[0], splitInts[1], splitInts[2]);
                                    }
                                    catch
                                    {
                                        await ReplyAndDeleteAsync("```INVALID R G B STRUCTURE!```",
                                            timeout: TimeSpan.FromSeconds(3));
                                    }

                                    break;

                                case "description":
                                    await instructions.ModifyAsync(x => { x.Content = "Enter Description text:"; });
                                    response = await NextMessageAsync();
                                    if (response != null)
                                    {
                                        embedDescription = response.Content;
                                    }

                                    break;

                                case "image":
                                    await instructions.ModifyAsync(x => { x.Content = "Enter Image URL:"; });
                                    response = await NextMessageAsync();
                                    if (response != null)
                                    {
                                        if (Uri.IsWellFormedUriString(response.Content, UriKind.Absolute))
                                        {
                                            embedImageURL = response.Content;
                                        }
                                        else
                                        {
                                            await ReplyAndDeleteAsync("```INVALID URL!```", timeout: TimeSpan.FromSeconds(3));
                                        }
                                    }

                                    break;

                                case "field":
                                    await instructions.ModifyAsync(x => { x.Content = "Enter Field Name text:"; });

                                    response = await NextMessageAsync();
                                    if (response != null)
                                    {
                                        string fTitle = response.Content;
                                        await response.DeleteAsync();

                                        await instructions.ModifyAsync(x => { x.Content = "Enter Field Content text:"; });

                                        response = await NextMessageAsync();
                                        if (response != null)
                                        {
                                            string fContent = response.Content;
                                            await response.DeleteAsync();

                                            await instructions.ModifyAsync(x => { x.Content = "Inline? [T]rue or [F]alse?"; });
                                            Boolean fInline = false;

                                            response = await NextMessageAsync();
                                            if (response != null)
                                            {
                                                if (response.Content.ToLower().StartsWith("t"))
                                                    fInline = true;

                                                fieldBuilder.Add(new EmbedFieldBuilder
                                                {
                                                    Name = fTitle,
                                                    Value = fContent,
                                                    IsInline = fInline
                                                });
                                            }
                                        }
                                    }

                                    break;

                                case "footer text":
                                    await instructions.ModifyAsync(x => { x.Content = "Enter Footer text:"; });
                                    response = await NextMessageAsync();
                                    if (response != null)
                                    {
                                        footText = response.Content;
                                    }

                                    break;

                                case "submit":
                                    submit = true;
                                    await preview.DeleteAsync();
                                    await instructions.DeleteAsync();
                                    await pic.DeleteAsync();
                                    run = false;
                                    valid = false;
                                    break;

                                case "cancel":
                                    await preview.DeleteAsync();
                                    await instructions.DeleteAsync();
                                    await pic.DeleteAsync();
                                    run = false;
                                    valid = false;
                                    break;
                                default:
                                    await ReplyAndDeleteAsync(
                                        "```UNKNOWN OPTION. PLEASE ENTER ONLY THE OPTIONS LISTED ABOVE.\nFor example \"title\"```",
                                        timeout: TimeSpan.FromSeconds(5));
                                    valid = false;
                                    break;
                            }

                            if (valid) //Unknown command was sent. Don't delete.
                            {
                                try
                                {
                                    await response.DeleteAsync();
                                }
                                catch
                                {
                                }
                            }

                            authBuilder = new EmbedAuthorBuilder()
                            {
                                Name = authName,
                                IconUrl = Context.Message.Author.GetAvatarUrl()
                            };
                            footBuilder = new EmbedFooterBuilder()
                            {
                                Text = footText,
                                IconUrl = Context.Message.Author.GetAvatarUrl()
                            };
                            builder = new EmbedBuilder()
                            {
                                Fields = fieldBuilder,
                                Footer = footBuilder,
                                Author = authBuilder,
                                ImageUrl = embedImageURL,
                                Url = embedURL,
                                Title = embedTitle,
                                ThumbnailUrl = embedThumbUrl,
                                Color = embedColor,
                                Description = embedDescription
                            };
                            if (valid)
                            {
                                await preview.ModifyAsync(x =>
                                {
                                    x.Content = "__**PREVIEW**__";
                                    x.Embed = builder.Build();
                                });
                                await instructions.ModifyAsync(x => { x.Content = instructionsStr; });
                            }
                        }
                        else
                        {
                            await ReplyAsync("```Announce Builder Timed out after 120 seconds!!```");
                            await instructions.DeleteAsync();
                            await pic.DeleteAsync();
                            await preview.DeleteAsync();
                        }
                    }
                }

                if(submit)
                {
                    var msg = await ReplyAsync("Send this to what channel?", false, builder);
                    Boolean sent = false;
                    while (!sent)
                    {
                        var response = await NextMessageAsync();
                        if (response != null)
                        {
                            if (response.Content.ToLower() == "cancel")
                                return;

                            foreach (SocketTextChannel s in Context.Guild.TextChannels)
                            {
                                if (s.Name.ToLower() == response.Content)
                                {
                                    await s.SendMessageAsync("", false, builder);
                                    await _dataServices.ChannelLog($"Embed created by {Context.User} was sent to {s.Name}!");
                                    await _dataServices.logChannel.SendMessageAsync("", false, builder);
                                    sent = true;
                                    await msg.ModifyAsync(x =>
                                    {
                                        x.Content = "__**SENT!**__";
                                        x.Embed = builder.Build();
                                    });
                                    await response.DeleteAsync();
                                    return;
                                }
                            }
                            await ReplyAndDeleteAsync("```CHANNEL NOT FOUND TRY AGAIN.```", timeout: TimeSpan.FromSeconds(3));
                            await response.DeleteAsync();
                        }
                        else
                        {
                            await msg.DeleteAsync();
                            await ReplyAsync("```Announce Builder Timed out after 120 seconds!!```");
                        }
                    }
                }

                if (quickSendChannel != null)
                {
                    bool sent = false;
                    foreach (SocketTextChannel s in Context.Guild.TextChannels)
                    {
                        if (s.Name.ToLower() == quickSendChannel)
                        {
                            await s.SendMessageAsync("", false, builder);
                            await _dataServices.ChannelLog($"Embed created by {Context.User} was sent to {s.Name}!");
                            await _dataServices.logChannel.SendMessageAsync("", false, builder);
                            sent = true;
                        }
                    }

                    if (!sent)
                    {
                        await ReplyAndDeleteAsync("```CHANNEL NOT FOUND```", timeout: TimeSpan.FromSeconds(3));
                    }
                }
            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} is trying to announce from the bot.");
                await ReplyAsync("```You cannot use this command with your current permission level!```");
            }
        }

        private bool isValidTag(string inString, Regex regex)
        {
            string[] validTags = { "{author name}", "{thumbnail}", "{title}", "{url}", "{color}", "{description}", "{image}", "{footer text}", "{field}", "{}", "{submit}" };
            MatchCollection matches = regex.Matches(inString);
            matches.Cast<Match>().Select(m => m.Value).Distinct().ToList();
            foreach (var m in matches)
            {
                if (!validTags.Contains(m.ToString().ToLower()))
                    return false;
            }
            return true;
        }

        [Command("rcon")]
        [Summary("`>rcon [server] [command]` Sends rcon command to server.")]
        [Remarks("Requirements: Moderator Role. Sends rcon command to the desired server. Use the server 3 letter code (ex: `eus`) to pick the server. If " +
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

                    //Fire and forget. Start the post tasks and don't wait for them to complete.
                    Task fireAndForget = PostTasks(server);

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

        private async Task PostTasks(JsonServer server)
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
            await _dataServices.RconCommand($"sv_voiceenable 0; say Please join the Level Testing voice channel for feedback!", server);
            await Task.Delay(3000);
            await _dataServices.RconCommand($"say Please join the Level Testing voice channel for feedback!", server);
            await Task.Delay(3000);
            await _dataServices.RconCommand($"say Please join the Level Testing voice channel for feedback!", server);
            await Task.Delay(3000);
            await _dataServices.RconCommand($"say Please join the Level Testing voice channel for feedback!", server);

            // Starts downloading playtesting files in the background.
            _downloaderService.Start(_mod.TestInfo, server);

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
        [Summary("`>shutdown` shuts down the bot")]
        [Remarks("Requirements: Moderator Role.")]
        public async Task ShutdownAsync()
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("**This command can not be used in a DM**");
                return;
            }

            if ((Context.User as SocketGuildUser).Roles.Contains(_dataServices.ModRole))
            {
                await Context.Message.DeleteAsync();
                await _dataServices.ChannelLog($"Shutting down! Invoked by {Context.Message.Author}");
                await Task.Delay(1000);
                Environment.Exit(0);
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
                    _timer.Restart();
                }
            }
            else
            {
                await _dataServices.ChannelLog($"{Context.User} is trying to shutdown the bot.");
                await ReplyAsync("```You cannot use this command with your current permission level!```");
            }
        }

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
        [Remarks("`>cr [server prefix]` will clear a specific server. Requirements: Moderator Role")]
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
