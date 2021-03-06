using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using BotHATTwaffle.Commands.Preconditions;
using BotHATTwaffle.Extensions;
using BotHATTwaffle.Models;
using BotHATTwaffle.Services;
using BotHATTwaffle.Services.Download;
using BotHATTwaffle.Services.Embed;
using BotHATTwaffle.Services.Playtesting;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace BotHATTwaffle.Commands
{
    public class ModerationModule : InteractiveBase
    {
        private readonly DiscordSocketClient _client;
        private readonly DataService _data;
        private readonly DownloadService _download;
        private readonly PlaytestingService _playtesting;
        private readonly IMuteService _mute;
        private readonly ITimerService _timer;

        private static string[] _testInfo;

        public ModerationModule(
            DiscordSocketClient client,
            DataService data,
            DownloadService download,
            PlaytestingService playtesting,
            IMuteService mute,
            ITimerService timer)
        {
            _client = client;
            _data = data;
            _download = download;
            _playtesting = playtesting;
            _mute = mute;
            _timer = timer;
        }

        [Command("Suppress")]
        [Summary("Toggles the bot's announce flags for firing timed alerts for playtests.")]
        [Remarks("Different inputs will suppress different flags.\n1 = Hour\n2 = Twenty\n3 = Fifteen\n4 = Start\n5 = All")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task SuppressAsync(int input = 0)
        {
            if (input == 0)
            {
                await ReplyAsync($"```True means this alert will not be fired until the announcement " +
                                 $"message has changed.\n\nCurrent Alert Flags:\n{_playtesting.GetAnnounceFlags()}```");

                return;
            }

            _playtesting.SuppressAnnounce(input);

            await ReplyAsync($"```True means this alert will not be fired until the announcement " +
                             $"message has changed.\n\nCurrent Alert Flags:\n{_playtesting.GetAnnounceFlags()}```");
            await _data.ChannelLog($"{Context.User} changed playtest alert flag suppression", _playtesting.GetAnnounceFlags());

            await DataBaseUtil.AddCommandAsync("Suppress", Context);
        }

        [Command("Announce", RunMode = RunMode.Async)]
        [Summary("Interactively create an embed message to be sent to any channel.")]
        [Remarks(
            "The entire embed can also be built at once using the following template:```{Author Name}{Thumbnail}{Title}{URL}" +
            "{Color}{Description}{Image}{Footer Text}{Field}{}{}{Submit}```\n" +
            "Example:```{Author Name}myAuthName{Thumbnail}http://www.myThumb.com{Title}myTitle{URL}http://www.myURL.com{Color}" +
            "255 100 50{Description}myDesc{Image}http://www.myImg.com{Footer Text}myFooter{Field}myFieldtitle{}myFieldText{}(t" +
            "|f){submit}general```\n" +
            "Fields can be omitted. Multiple fields can be added simultaneously.")]
        [Alias("a")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task AnnounceAsync(
            [Summary("A format string or the input for the interactive builder's current prompt.")] [Remainder]
            string input = null)
        {
            await Context.Message.DeleteAsync();

            if (input != null)
            {
                // Builds the embed from a supplied formatting string.
                var builder = new QuickBuilder(Context);
                Embed embed = builder.Build(input);

                if (!string.IsNullOrWhiteSpace(builder.Errors))
                    await ReplyAndDeleteAsync($"```{builder.Errors}```", timeout: TimeSpan.FromSeconds(15));

                if (embed == null) return; // Builder was cancelled.

                await SendEmbedAsync(embed, await builder.ParseChannels());
            }
            else
            {
                // No formatting string given; interactively builds the embed by prompting the user.
                var builder = new InteractiveBuilder(Context, Interactive);
                Embed embed = await builder.BuildAsync();

                if (embed == null) return; // Builder was cancelled or timed out.

                await SendEmbedAsync(embed, await builder.PromptDestinationAsync(Context));
            }

            // Helper function the send the embed to all given channels.
            async Task SendEmbedAsync(Embed embed, IReadOnlyCollection<SocketTextChannel> channels)
            {
                if (!channels.Any())
                {
                    await ReplyAndDeleteAsync("```No channel mentions were found.```", timeout: TimeSpan.FromSeconds(15));

                    return;
                }

                foreach (SocketTextChannel channel in channels)
                    await channel.SendMessageAsync(string.Empty, false, embed);

                await _data.ChannelLog(
                    $"Embed created by {Context.User} was sent to {string.Join(", ", channels.Select(c => c.Name))}.");
                await _data.LogChannel.SendMessageAsync(string.Empty, false, embed);
            }

            await DataBaseUtil.AddCommandAsync("Announce", Context);
        }

        [Command("Rcon")]
        [Summary("Invokes an RCON command on a server.")]
        [Remarks("The command's output, if any, will be displayed by the bot.")]
        [Alias("r")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators, Role.RconAccess)]
        public async Task RconAsync(
            [Summary("The three-letter code which identifies the server on which to invoke the command.")]
            string serverCode,
            [Summary("The command to invoke on the server.")] [Remainder]
            string command)
        {
            // Command blacklist.
            // TODO: Move to a config file.
            if (command.Contains("rcon_password", StringComparison.OrdinalIgnoreCase ) ||
                command.Contains("exit", StringComparison.OrdinalIgnoreCase ))
            {
                await ReplyAsync("```This command cannot be run from here. Ask TopHATTwaffle to do it.```");
                await _data.ChannelLog($"{Context.User} was trying to run a blacklisted command", $"{command} was trying to be sent to {serverCode}");

                return;
            }

            Server server = await _data.GetServer(serverCode);

            if (server == null)
            {
                await ReplyAsync(
                    $"```The command was not sent because the server '{serverCode}' could not be found.```");

                return;
            }

            string reply;

            try
            {
                reply = await _data.RconCommand(command, server);

                // Remove log messages from the log.
                string[] replyArray = reply.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
                reply = string.Join("\n", replyArray.Where(x => !x.Trim().StartsWith("L ")));
                reply = reply.Replace("discord.gg", "discord,gg").Replace(server.rcon_password, "[PASSWORD HIDDEN]");

                if (reply.Length > 1880)
                    reply = $"{reply.Substring(0, 1880)}\n[OUTPUT OMITTED...]";
            }
            catch
            {
                await ReplyAsync("```The command was not sent because an exception was thrown.```");

                return;
            }

            if (reply == "HOST_NOT_FOUND")
            {
                await ReplyAsync(
                    "```The command could not be send because the server's IP address could not be found\n" +
                    "This is a probably a DNS issue.```");
            }
            else
            {
                if (command.Contains("sv_password", StringComparison.OrdinalIgnoreCase ))
                {
                    await Context.Message.DeleteAsync();

                    await ReplyAsync($"```Command sent to {server.name}\nA password was set on the server.```");
                    await _data.ChannelLog(
                        $"{Context.User} Sent RCON command",
                        $"A password command was sent to: {server.address}");
                }
                else
                {
                    await ReplyAsync($"```{command} sent to {server.name}\n{reply}```");
                    await _data.ChannelLog(
                        $"{Context.User} Sent RCON command",
                        $"{command} was sent to: {server.address}\n{reply}");
                }
            }

            await DataBaseUtil.AddCommandAsync("Rcon", Context);
        }

        [Command("Playtest")]
        [Summary("Preforms an action on a server.")]
        [Remarks(
            "Actions:\n" +
            "`pre` - Sets the testing config and reloads the map to clear cheats.\n" +
            "`start` - Starts the playtest, starts recording a demo, and then tells the server it is live.\n" +
            "`post` - Starts the postgame config. Gets the playtest's demo and BSP files and stores them in the public " +
            "DropBox folder.\n" +
            "`scramble`, `s` - Scrambles the teams.\n" +
            "`pause`, `p` - Pauses the playtest.\n" +
            "`unpause`, `u` - Resumes the playtest.\n\n" +
            "If no server is specified, the event server is used.")]
        [Alias("p")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task PlaytestAsync(
            [Summary("The action to perform on the server.")]
            string action,
            [Summary("The three-letter code which identifies the server on which to perform the action.")]
            string serverCode = null)
        {
            if (_playtesting.CurrentEventInfo[0] == "NO_EVENT_FOUND")
            {
                await ReplyAsync("```Cannot use this command unless a test is scheduled.```");
                return;
            }
            string config;
            string gameMode = _playtesting.CurrentEventInfo[7];

            // Gets the given server. Otherwise, uses the current event's server.
            Server server = await _data.GetServer(serverCode ?? _playtesting.CurrentEventInfo[10].Substring(0, 3));

            if (gameMode.Equals("competitive", StringComparison.OrdinalIgnoreCase ) ||
                gameMode.Equals("comp", StringComparison.OrdinalIgnoreCase ))
            {
                config = _data.CompConfig;
            }
            else
                config = _data.CasualConfig; // If not comp, casual.

            if (action.Equals("pre", StringComparison.OrdinalIgnoreCase ))
            {
                _testInfo = _playtesting.CurrentEventInfo; // Stores the test info for later use in retrieving the demo.
                string workshopId = Regex.Match(_playtesting.CurrentEventInfo[6], @"\d+$").Value;

                await _data.ChannelLog($"Playtest Prestart on {server.name}", $"exec {config}" +
                    $"\nhost_workshop_map {workshopId}");

                await _data.RconCommand($"exec {config}", server);
                await Task.Delay(1000);

                await _data.RconCommand($"host_workshop_map {workshopId}", server);

                await ReplyAsync($"```Playtest Prestart on {server.name}\nexec {config}\nhost_workshop_map {workshopId}```");
            }
            else if (action.Equals("start", StringComparison.OrdinalIgnoreCase ))
            {
                _testInfo = _playtesting.CurrentEventInfo; // Stores the test info for later use in retrieving the demo.

                DateTime time = Convert.ToDateTime(_playtesting.CurrentEventInfo[1]);
                string title = _playtesting.CurrentEventInfo[2].Split(new[] { ' ' }, 2).FirstOrDefault() ?? string.Empty;
                string demoName = $"{time:MM_dd_yyyy}_{title}_{gameMode}";

                await ReplyAsync($"```Playtest Start on {server.name}\nexec {config}\ntv_record {demoName}```");
                await _data.ChannelLog(
                    $"Playtest Start on {server.name}",
                    $"exec {config}\ntv_record {demoName}\nsay Playtest of {title} is now live! Be respectiful and GLHF!");

                await _data.RconCommand($"exec {config}", server);
                await Task.Delay(3250);

                await _data.RconCommand($"tv_record {demoName}", server);
                await Task.Delay(1000);

                await _data.RconCommand($"say Demo started! {demoName}", server);
                await Task.Delay(1000);

                await _data.RconCommand($"say Playtest of {title} is now live! Be respectful and GLHF!", server);
                await Task.Delay(1000);

                await _data.RconCommand($"say Playtest of {title} is now live! Be respectful and GLHF!", server);
                await Task.Delay(1000);

                await _data.RconCommand($"say Playtest of {title} is now live! Be respectful and GLHF!", server);
            }
            else if (action.Equals("post", StringComparison.OrdinalIgnoreCase ))
            {
                Task _ = PostTasks(server); // Fired and forgotten.

                await ReplyAsync("```Playtest post started. Begin feedback!```");
                await _data.ChannelLog(
                    $"Playtest Post on {server.name}",
                    $"exec {_data.PostConfig}\nsv_voiceenable 0\nGetting Demo and BSP file and moving into DropBox");
            }
            else if (action.Equals("scramble", StringComparison.OrdinalIgnoreCase ) ||
                     action.Equals("s", StringComparison.OrdinalIgnoreCase ))
            {
                await _data.RconCommand("mp_scrambleteams 1", server);

                await ReplyAsync($"```Playtest Scramble on {server.name}\nmp_scrambleteams 1```");
                await _data.ChannelLog($"Playtest Scramble on {server.name}", "mp_scrambleteams 1");
            }
            else if (action.Equals("pause", StringComparison.OrdinalIgnoreCase ) ||
                     action.Equals("p", StringComparison.OrdinalIgnoreCase ))
            {
                await _data.RconCommand(@"mp_pause_match; say Pausing Match", server);

                await ReplyAsync($"```Playtest Scramble on {server.name}\nmp_pause_match```");
                await _data.ChannelLog($"Playtest Pause on {server.name}", "mp_pause_match");
            }
            else if (action.Equals("unpause", StringComparison.OrdinalIgnoreCase ) ||
                     action.Equals("u", StringComparison.OrdinalIgnoreCase ))
            {
                await _data.RconCommand(@"mp_unpause_match; say Unpausing Match", server);

                await ReplyAsync($"```Playtest Unpause on {server.name}\nmp_unpause_match```");
                await _data.ChannelLog($"Playtest Unpause on {server.name}", "mp_unpause_match");
            }
            else
            {
                await ReplyAsync(
                    "Invalid action, please try:\n`pre`\n`start`\n`post`\n`scramble` or `s`\n`pause` or `p`\n`unpause` or `u`");
            }

            await DataBaseUtil.AddCommandAsync("Playtest", Context);
        }

        /// <summary>
        /// Post takes are here so we can just fire and forget them. Nothing relies on them so we can forget about waiting.
        /// </summary>
        /// <param name="server">The server on which the event was hosted.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        // TODO: Move to a service.
        private async Task PostTasks(Server server)
        {
            string workshopId = Regex.Match(_testInfo[6], @"\d+$").Value;

            await _data.RconCommand($"host_workshop_map {workshopId}", server);
            await Task.Delay(15000);

            await _data.RconCommand(
                $"exec {_data.PostConfig}; say Please join the Level Testing voice channel for feedback!",
                server);
            await Task.Delay(3000);

            await _data.RconCommand(
                "sv_voiceenable 0; say Please join the Level Testing voice channel for feedback!",
                server);
            await Task.Delay(3000);

            await _data.RconCommand(
                "sv_cheats 1; say Please join the Level Testing voice channel for feedback!",
                server);
            await Task.Delay(3000);

            await _data.RconCommand("sv_full_alltalk 1; say Please join the Level Testing voice channel for feedback!", server);
            await Task.Delay(3000);

            await _data.RconCommand("say Please join the Level Testing voice channel for feedback!", server);

            // Starts downloading playtesting files in the background.
            _download.Start(_testInfo, server);

            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = $"Download Playtest Demo for {_testInfo[2]}",
                    IconUrl = _client.Guilds.FirstOrDefault()?.IconUrl,
                },
                Url = "http://demos.tophattwaffle.com",
                Title = "Download Here",
                ThumbnailUrl = _testInfo[4],
                Color = new Color(243, 128, 72),
                Description = "You can get the demo for this playtest by clicking above!\n\n*Thanks for testing with us!*\n\n" +
                              $"[Map Images]({_testInfo[5]}) | [Schedule a Playtest]" +
                              "(https://www.tophattwaffle.com/playtesting/) | [View Testing Calendar]" +
                              "(http://playtesting.tophattwaffle.com)"
            };

            string[] splitUser = _testInfo[3].Split('#');
            SocketUser author = _client.GetUser(splitUser[0], splitUser[1]);

            if (author == null)
            {
                // Author's user could not be found.
                await _data.TestingChannel.SendMessageAsync(
                    $"Hey {_testInfo[3]}! Next time you submit for a playtest, make sure to include your full Discord name so " +
                    "I can mention you. You can download your demo here:");
            }
            else
            {
                try
                {
                    // Tries to send a DM.
                    await author.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch
                {
                    // Mentions the author in the playtesting channel instead.
                    await _data.TestingChannel.SendMessageAsync($"{author.Mention} You can download your demo here:");
                }
            }

            await _data.TestingChannel.SendMessageAsync(string.Empty, false, embed.Build());
        }

        [Command("Shutdown", RunMode = RunMode.Async)]
        [Summary("Shuts down the bot.")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task ShutdownAsync()
        {
            await DataBaseUtil.AddCommandAsync("Shutdown", Context);

            await Context.Message.DeleteAsync();
            await _data.ChannelLog($"{Context.Message.Author} is shutting down the bot.");

            await Context.Client.SetStatusAsync(UserStatus.Invisible); // Workaround for an issue with StopAsync.
            await Context.Client.StopAsync();
            await Task.Delay(2000); // Without the delay, the bot never logs the shutdown.
            Environment.Exit(0);
        }

        [Command("Reload")]
        [Summary("Reloads data from settings files.")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task ReloadAsync()
        {
            try
            {
                await _data.DeserialiseConfig(true);
                _timer.Stop();
                _timer.Start();

                await ReplyAsync("```Successfully reloaded settings.```");
                await _data.ChannelLog($"{Context.User} successfully reloaded the settings.");
            }
            catch (InvalidOperationException e)
            {
                await _data.ChannelLog($"{Context.User} failed to reload the settings.", e.Message);
                await Context.Channel.SendMessageAsync(
                    $"An error occurred reloading the config. Reverting to the previous config. ```{e.Message}```");
            }

            await DataBaseUtil.AddCommandAsync("Reload", Context);
        }

        [Command("DumpSettings")]
        [Summary("Dumps currently loaded settings to a DM.")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task DumpSettingsAsync()
        {
            await Context.Message.DeleteAsync();

            string reply = string.Join("\n", _data.Config.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            reply = reply.Replace(_data.Config["botToken"], "[REDACTED]");

            try
            {
                await Context.Message.Author.SendMessageAsync($"```{reply}```");
            }
            catch
            {
                // Do nothing if the user doesn't accept DMs.
            }

            await _data.ChannelLog($"{Context.User} dumped the settings.");

            await DataBaseUtil.AddCommandAsync("DumpSettings", Context);
        }

        [Command("Mute")]
        [Summary("Mutes a user.")]
        [Remarks(
            "Specify a duration of `0` for an indefinite mute.\n" +
            "Only supports integer durations because expired mutes are checked at an interval of one minute.")]
        [Alias("m")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task MuteAsync(
            [Summary("The user to mute.")] SocketGuildUser user,
            [Summary("The duration, in minutes, of the mute.")]
            [RequireBoundaries(1, 43200)] int duration = 5,
            [Summary("The reason for the mute.")] [Remainder]
            string reason = "No reason provided.")
        {
            if (user.Roles.Contains(_data.ModRole))
            {
                await Context.Channel.SendMessageAsync(string.Empty, embed:
                    new EmbedBuilder().WithAuthor("Mods don't mute other Mods...")
                        .WithDescription("Now you 2 need to learn to play nice and get along."));

                return;
            }

            if (await _mute.MuteAsync(user, (SocketGuildUser)Context.User, duration == 0 ? default(int?) : duration, reason))
                await Context.Channel.SendMessageAsync($"Successfully muted {user}.");
            else
                await Context.Channel.SendMessageAsync($"{user} is already muted!");

            await DataBaseUtil.AddCommandAsync("Mute", Context);
        }

        [Command("Unmute")]
        [Summary("Unmutes a user.")]
        [Remarks("Lists all active mutes if invoked without arguments.")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task UnmuteAsync(
            [Summary("The user to unmute.")] SocketGuildUser user = null,
            [Summary("The reason for the unmute.")] [Remainder]
            string reason = "A moderator has taken mercy on you by lifting the mute.")
        {
            if (user != null)
            {
                if (await _mute.UnmuteAsync(user, (SocketGuildUser)Context.User, reason))
                    await ReplyAsync($"Unmuted {user}.");
                else
                    await ReplyAsync($"Failed to unmute {user} because the user isn't muted.");
            }
            else
            {
                string reply = null;

                foreach (Mute mute in await DataBaseUtil.GetActiveMutesAsync())
                {
                    //TODO: Move this to >MuteHistory. It is only here because I'm too lazy and this was easier...
                    reply += $"Name: {mute.Username}\nMuted at: {mute.Timestamp:yyyy-MM-ddTHH:mm:ssZ}\n" +
                             $"Duration: {mute.Duration?.ToString() ?? "indefinite"}\nReason: {mute.Reason ?? "None"}\n" +
                             $"Expires at: {mute.Timestamp.AddMinutes(mute.Duration ?? 0)}";
                }

                if (reply == null)
                    reply = "No mutes found!";

                await ReplyAsync($"Current Mutes: {reply}");
            }

            await DataBaseUtil.AddCommandAsync("Unmute", Context);
        }

        [Command("MuteHistory")]
        [Summary("Retrieves the mute history of a user.")]
        [Remarks("Returns up to 25 of the most recent mutes (including an ongoing mute, if applicable).")]
        [Alias("MuteStatus", "mh")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task MuteHistoryAsync(
            [Summary("The user for which to retrieve mute history.")] SocketGuildUser user,
            [RequireBoundaries(1, 25)] int quantity = 10)
        {
            Mute[] mutes = await DataBaseUtil.GetMutesAsync(user.Id, quantity);
            int total = mutes.Length;
            var pages = new List<EmbedBuilder>();

            await DataBaseUtil.AddCommandAsync("MuteHistory", Context);

            if (mutes.Any())
                await BuildPage();
            else
            {
                var embed = new EmbedBuilder
                {
                    ThumbnailUrl = user.GetAvatarUrl(),
                    Color = new Color(243, 128, 72)
                };

                embed.WithAuthor($"No mutes for {user}!");
                embed.Description = "Who knew we have users that could behave!";

                await ReplyAsync(string.Empty, embed: embed.Build());

                return;
            }

            async Task BuildPage(EmbedFieldBuilder firstField = null)
            {
                var embed = new EmbedBuilder
                {
                    ThumbnailUrl = user.GetAvatarUrl(),
                    Color = new Color(243, 128, 72)
                };

                embed.WithAuthor($"{user}'s Most Recent Mutes");

                if (firstField != null)
                    embed.AddField(firstField);

                foreach (Mute mute in mutes)
                {
                    string timestamp = mute.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    string value = $"Muted by: `{mute.MuterName}`";
                    string valueEnd;

                    if (mute.Duration.HasValue)
                    {
                        string units = mute.Duration > 1 ? "minutes" : "minute";
                        DateTimeOffset unmuted = mute.Timestamp.AddMinutes(mute.Duration.Value);
                        valueEnd = $"\nDuration: `{mute.Duration}` {units}\nUnmuted at: `{unmuted:yyyy-MM-ddTHH:mm:ssZ}`";
                    }
                    else
                        valueEnd = "\nDuration: `indefinite`";

                    string reason = null;

                    if (mute.Reason != null)
                    {
                        value += "\nReason: `";
                        valueEnd = "`" + valueEnd;
                        reason = mute.Reason.Truncate(1024 - value.Length - valueEnd.Length, true);
                    }

                    embed.AddField(timestamp, value + reason + valueEnd);

                    if (embed.Length() > 6000 - 26) // Total char limit - maximum possible footer length.
                    {
                        EmbedFieldBuilder field = embed.Fields.Pop(); // Re-use the field in the next embed.
                        pages.Add(embed);

                        mutes = mutes.Skip(embed.Fields.Count + 1).ToArray(); // Skips already processed records.
                        await BuildPage(field); // Process the remaining records.

                        return;
                    }
                }

                pages.Add(embed);
            }

            // Sets the footer text and sends each embed.
            if (pages.Count > 1)
            {
                for (var i = 0; i < pages.Count;)
                {
                    EmbedBuilder embed = pages[i];
                    embed.WithFooter($"{total} Results | Page {++i} of {pages.Count}");

                    await ReplyAsync(string.Empty, embed: embed.Build());
                }
            }
            else
                await ReplyAsync(string.Empty, embed: pages.Single().Build());
        }

        [Command("ClearReservations")]
        [Summary("Clears server reservations.")]
        [Remarks("If no server is specified, _all_ server reservations are cleared.")]
        [Alias("cr")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task ClearReservationsAsync(
            [Summary("The three-letter code which identifies the server to clear.")]
            string serverCode = null)
        {
            if (serverCode == null)
                await _playtesting.ClearServerReservations();
            else
                await _playtesting.ClearServerReservations(serverCode);

            await ReplyAsync(string.Empty, false, _playtesting.DisplayServerReservations());

            await DataBaseUtil.AddCommandAsync("ClearReservations", Context);
        }

        [Command("Active")]
        [Summary("Grants a user the Active Memeber role")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task ActiveAsync([Summary("User to give role to")]SocketGuildUser user)
        {
            await _data.ChannelLog($"{user} has been given {_data.ActiveRole.Mention} by {Context.User}");
            await ReplyAsync($"{user.Mention} has been given {_data.ActiveRole.Mention}!\n\nThanks for being an active member in our community!");
            await ((IGuildUser)user).AddRoleAsync(_data.ActiveRole);

            await DataBaseUtil.AddCommandAsync("Active", Context);
        }

        //TODO: Quick and dirty command to allow us to manage servers without requiring manual DB editing. Improve later.
        [Command("EditServers")]
        [Summary("Allows editing of servers inside the database.")]
        [Remarks("Commands:\n`Add`\n`Get`\n`Remove`\nUse the following template to add a server:" +
                 "```>EditServers Add name|description|address|rcon_password|ftp_path|ftp_username|ftp_password|(ftp,ftps,sftp)```" +
                 "To remove a server: `>EditServers Remove [ServerCode]`\n" +
                 "To Get a servers: `>EditServers Get [ServerCode]`")]
        [RequireContext(ContextType.Guild)]
        [RequireRole(Role.Moderators)]
        public async Task EditServers(string action = null, [Remainder]string values = null)
        {
            if (!_playtesting.CanReserve)
            {
                //Can't reserve, meaning we are close to a test. Don't want to edit fields.
                await ReplyAsync("You cannot edit servers this close to a playtest. Try again later.");
                return;
            }

            if(action.StartsWith("a", StringComparison.OrdinalIgnoreCase))
            {
                string[] vars = values.Split('|');

                if (vars.Length != 8)
                {
                    await ReplyAsync("You didn't provide all 8 parameters. Please provide all 8 and try again." +
                                     "\nYour message was deleted as it may have contained a password.");
                    await Context.Message.DeleteAsync();
                    return;
                }

                if (vars[0].Length != 3)
                {
                    //Enforce length during the add because SQLite does not support min/max lengths on TEXT fields.
                    await ReplyAsync("Server name must be 3 characters long. Please provide 3 characters as the name and try again." +
                                     "\nYour message was deleted as it may have contained a password.");
                    await Context.Message.DeleteAsync();
                    return;
                }

                //Validate FTP type before entry
                switch(vars[7])
                {
                    case "ftp":
                        break;
                    case "sftp":
                        break;
                    case "ftps":
                        break;
                    default:
                    await ReplyAsync("Invalid FTP type. Please provide `ftp`, `ftps`, or `sftp` and try again." +
                                     "\nYour message was deleted as it may have contained a password.");
                    await Context.Message.DeleteAsync();
                    return;
                }

                await DataBaseUtil.AddServerAsync(new Server()
                {
                    name = vars[0],
                    description = vars[1],
                    address = vars[2],
                    rcon_password = vars[3],
                    ftp_path = vars[4],
                    ftp_username = vars[5],
                    ftp_password = vars[6],
                    ftp_type = vars[7]
                });

                await ReplyAsync("Server added! Your message was removed as it contained passwords.");
                await Context.Message.DeleteAsync();
            }
            else if (action.StartsWith("g", StringComparison.OrdinalIgnoreCase))
            {
                var server = await DataBaseUtil.GetServerAsync(values.Substring(0, 3));

                if(server != null)
                    await ReplyAsync($"`{server.ToString()}`");
                else
                    await ReplyAsync($"I could not find a server with that name.");
            }
            else if (action.StartsWith("r", StringComparison.OrdinalIgnoreCase))
            {
                Server removedServer = await DataBaseUtil.GetServerAsync(values.Substring(0, 3));

                if (removedServer != null)
                {
                    await ReplyAsync($"Removing server\n`{removedServer.ToString()}`");
                    await DataBaseUtil.RemoveServerAsync(removedServer);
                }
                else
                {
                    await ReplyAsync($"I could not find a server with that name.");
                    return;
                }

            }
            else
                await ReplyAsync("Invalid command. Please see the help text.");

            await DataBaseUtil.AddCommandAsync("EditServers", Context);
        }
    }
}
