using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using BotHATTwaffle.Objects;
using BotHATTwaffle.Objects.Downloader;
using BotHATTwaffle.Objects.Json;
using BotHATTwaffle.Services;
using BotHATTwaffle.Services.Embed;

namespace BotHATTwaffle.Modules
{
	public class ModerationModule : InteractiveBase
	{
		private readonly DiscordSocketClient _client;
		private readonly DataServices _data;
		private readonly DownloaderService _downloader;
		private readonly LevelTesting _levelTesting;
		private readonly IMuteService _mute;
		private readonly TimerService _timer;

		private string[] _testInfo;

		public ModerationModule(
			DiscordSocketClient client,
			DataServices data,
			DownloaderService downloader,
			LevelTesting levelTesting,
			IMuteService mute,
			TimerService timer)
		{
			_client = client;
			_data = data;
			_downloader = downloader;
			_levelTesting = levelTesting;
			_mute = mute;
			_timer = timer;
		}

		[Command("announce", RunMode = RunMode.Async)]
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
		}

		[Command("rcon")]
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

			LevelTestingServer server = _data.GetServer(serverCode);

			if (server == null)
			{
				await ReplyAsync(
					$"```The command was not sent because the server '{serverCode}' could not be found.\nIs it in the JSON?```");

				return;
			}

			string reply;

			try
			{
				reply = await _data.RconCommand(command, server);

				// Remove log messages from the log.
				string[] replyArray = reply.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
				reply = string.Join("\n", replyArray.Where(x => !x.Trim().StartsWith("L ")));
				reply = reply.Replace("discord.gg", "discord,gg").Replace(server.Password, "[PASSWORD HIDDEN]");

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

					await ReplyAsync($"```Command sent to {server.Name}\nA password was set on the server.```");
					await _data.ChannelLog(
						$"{Context.User} Sent RCON command",
						$"A password command was sent to: {server.Address}");
				}
				else
				{
					await ReplyAsync($"```{command} sent to {server.Name}\n{reply}```");
					await _data.ChannelLog(
						$"{Context.User} Sent RCON command",
						$"{command} was sent to: {server.Address}\n{reply}");
				}
			}
		}

		[Command("playtest")]
		[Summary("Peforms an action on a server.")]
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
			if (_levelTesting.CurrentEventInfo[0] == "NO_EVENT_FOUND")
			{
				await ReplyAsync("```Cannot use this command unless a test is scheduled.```");
				return;
			}

			string config;
			string gameMode = _levelTesting.CurrentEventInfo[7];

			// Gets the given server. Otherwise, uses the current event's server.
			LevelTestingServer server = _data.GetServer(serverCode ?? _levelTesting.CurrentEventInfo[10].Substring(0, 3));

			if (gameMode.Equals("competitive", StringComparison.OrdinalIgnoreCase ) ||
			    gameMode.Equals("comp", StringComparison.OrdinalIgnoreCase ))
			{
				config = _data.CompConfig;
			}
			else
				config = _data.CasualConfig; // If not comp, casual.

			if (action.Equals("pre", StringComparison.OrdinalIgnoreCase ))
			{
				_testInfo = _levelTesting.CurrentEventInfo; // Stores the test info for later use in retrieving the demo.
				string workshopId = Regex.Match(_levelTesting.CurrentEventInfo[6], @"\d+$").Value;

				await _data.ChannelLog($"Playtest Prestart on {server.Name}", $"exec {config}" +
					$"\nhost_workshop_map {workshopId}");

				await _data.RconCommand($"exec {config}", server);
				await Task.Delay(1000);

				await _data.RconCommand($"host_workshop_map {workshopId}", server);

				await ReplyAsync($"```Playtest Prestart on {server.Name}\nexec {config}\nhost_workshop_map {workshopId}```");
			}
			else if (action.Equals("start", StringComparison.OrdinalIgnoreCase ))
			{
				_testInfo = _levelTesting.CurrentEventInfo; // Stores the test info for later use in retrieving the demo.

				DateTime time = Convert.ToDateTime(_levelTesting.CurrentEventInfo[1]);
				string title = _levelTesting.CurrentEventInfo[2].Split(new[] { ' ' }, 2).FirstOrDefault() ?? string.Empty;
				string demoName = $"{time:MM_dd_yyyy}_{title}_{gameMode}";

				await ReplyAsync($"```Playtest Start on {server.Name}\nexec {config}\ntv_record {demoName}```");
				await _data.ChannelLog(
					$"Playtest Start on {server.Name}",
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
					$"Playtest Post on {server.Name}",
					$"exec {_data.PostConfig}\nsv_voiceenable 0\nGetting Demo and BSP file and moving into DropBox");
			}
			else if (action.Equals("scramble", StringComparison.OrdinalIgnoreCase ) ||
			         action.Equals("s", StringComparison.OrdinalIgnoreCase ))
			{
				await _data.RconCommand("mp_scrambleteams 1", server);

				await ReplyAsync($"```Playtest Scramble on {server.Name}\nmp_scrambleteams 1```");
				await _data.ChannelLog($"Playtest Scramble on {server.Name}", "mp_scrambleteams 1");
			}
			else if (action.Equals("pause", StringComparison.OrdinalIgnoreCase ) ||
			         action.Equals("p", StringComparison.OrdinalIgnoreCase ))
			{
				await _data.RconCommand(@"mp_pause_match; say Pausing Match", server);

				await ReplyAsync($"```Playtest Scramble on {server.Name}\nmp_pause_match```");
				await _data.ChannelLog($"Playtest Pause on {server.Name}", "mp_pause_match");
			}
			else if (action.Equals("unpause", StringComparison.OrdinalIgnoreCase ) ||
			         action.Equals("u", StringComparison.OrdinalIgnoreCase ))
			{
				await _data.RconCommand(@"mp_unpause_match; say Unpausing Match", server);

				await ReplyAsync($"```Playtest Unpause on {server.Name}\nmp_unpause_match```");
				await _data.ChannelLog($"Playtest Unpause on {server.Name}", "mp_unpause_match");
			}
			else
			{
				await ReplyAsync(
					"Invalid action, please try:\n`pre`\n`start`\n`post`\n`scramble` or `s`\n`pause` or `p`\n`unpause` or `u`");
			}
		}

		/// <summary>
		/// Post takes are here so we can just fire and forget them. Nothing relies on them so we can forget about waiting.
		/// </summary>
		/// <param name="server">The server on which the event was hosted.</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		// TODO: Move to a service.
		private async Task PostTasks(LevelTestingServer server)
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

			await _data.RconCommand("say Please join the Level Testing voice channel for feedback!", server);
			await Task.Delay(3000);

			await _data.RconCommand("say Please join the Level Testing voice channel for feedback!", server);

			// Starts downloading playtesting files in the background.
			_downloader.Start(_testInfo, server);

			var embed = new EmbedBuilder
			{
				Author = new EmbedAuthorBuilder
				{
					Name = $"Download Playtest Demo for {_testInfo[2]}",
					IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png",
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

		[Command("shutdown", RunMode = RunMode.Async)]
		[Summary("Shuts down the bot.")]
		[RequireContext(ContextType.Guild)]
		[RequireRole(Role.Moderators)]
		public async Task ShutdownAsync()
		{
			await Context.Message.DeleteAsync();
			await _data.ChannelLog($"{Context.Message.Author} is shutting down the bot.");

			await Context.Client.SetStatusAsync(UserStatus.Invisible); // Workaround for an issue with StopAsync.
			await Context.Client.StopAsync();
			await Task.Delay(2000); // Without the delay, the bot never logs the shutdown.
			Environment.Exit(0);
		}

		[Command("reload")]
		[Summary("Reloads data from settings files.")]
		[RequireContext(ContextType.Guild)]
		[RequireRole(Role.Moderators)]
		public async Task ReloadAsync()
		{
			await ReplyAsync("```Reloading Settings!```");
			await _data.ChannelLog($"{Context.User} is reloading the bot's settings.");

			_data.ReloadSettings();
			_timer.Stop();
			_timer.Start();
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
		}

		[Command("mute")]
		[Summary("Mutes a user.")]
		[Remarks("Only supports integer durations because expired mutes are checked at an interval of one minute.")]
		[Alias("m")]
		[RequireContext(ContextType.Guild)]
		[RequireRole(Role.Moderators)]
		public async Task MuteAsync(
			[Summary("The user to mute.")] SocketGuildUser user,
			[Summary("The duration, in minutes, of the mute.")]
			int duration = 5,
			[Summary("The reason for the mute.")] [Remainder]
			string reason = "No reason provided.")
		{
			await _mute.MuteAsync(user, duration, Context.User, reason);
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
				await _levelTesting.ClearServerReservations();
			else
				await _levelTesting.ClearServerReservations(serverCode);

			await ReplyAsync(string.Empty, false, _levelTesting.DisplayServerReservations());
		}
	}
}
