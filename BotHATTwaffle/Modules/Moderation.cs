using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using BotHATTwaffle.Objects;
using BotHATTwaffle.Objects.Downloader;
using BotHATTwaffle.Objects.Json;

using Discord.Addons.Interactive;

namespace BotHATTwaffle.Modules
{
	public class ModerationService
	{
		private readonly DataServices _dataServices;
		private readonly List<UserData> _mutedUsers = new List<UserData>();

		public ModerationService(DataServices dataServices, TimerService timer)
		{
			_dataServices = dataServices;
			timer.AddHandler(CheckMutes);
		}

		/// <summary>
		/// Checks for expired or manually removed mutes and appropriately unmutes users.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">Provides data for the <see cref="Timer.Elapsed"/> event.</param>
		public async void CheckMutes(object sender, ElapsedEventArgs e)
		{
			foreach (UserData user in _mutedUsers.ToList())
			{
				if (!user.User.Roles.Contains(_dataServices.MuteRole))
				{
					await Unmute(user, $"The {_dataServices.MuteRole.Name} role was manually removed.");

					continue;
				}

				if (!user.MuteExpired()) continue;

				await Unmute(user, "The mute expired.");

				await Task.Delay(1000);
			}
		}

		/// <summary>
		/// Mutes a <paramref name="user"/> for a <paramref name="reason"/> and the given <paramref name="duration"/>.
		/// </summary>
		/// <param name="user">The user to mute.</param>
		/// <param name="duration">The duration, in minutes, of the mute.</param>
		/// <param name="mod">The user which issued the mute.</param>
		/// <param name="reason">The reason for the mute.</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		public async Task Mute(SocketGuildUser user, double duration, SocketUser mod, string reason = "")
		{
			DateTime expiration = DateTime.Now.AddMinutes(duration);

			_mutedUsers.Add(new UserData {User = user, MuteExpiration = expiration});
			await user.AddRoleAsync(_dataServices.MuteRole);

			await user.SendMessageAsync($"You were muted for {duration} minute because:\n{reason}.\n");
			await _dataServices.ChannelLog(
				$"{user} muted by {mod}",
				$"Muted for {duration} minutes (expires {expiration}) because:\n{reason}");
		}

		/// <summary>
		/// Unmutes a user with a <paramref name="reason"/> (for logging).
		/// </summary>
		/// <param name="user">The user to unmute.</param>
		/// <param name="reason">The reason for the unmute.</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		public async Task Unmute(UserData user, string reason = "")
		{
			if (!_mutedUsers.Remove(user)) return; // Attempts to remove the user. Returns if not muted.

			await user.User.RemoveRoleAsync(_dataServices.MuteRole); // No need to check if the user has the role.

			await user.User.SendMessageAsync("You have been unmuted!");
			await _dataServices.ChannelLog($"{user.User} unmuted", reason);
		}
	}

	public class ModerationModule : InteractiveBase
	{
		private readonly DiscordSocketClient _client;
		private readonly ModerationService _mod;
		private readonly LevelTesting _levelTesting;
		private readonly DataServices _dataServices;
		private readonly TimerService _timer;
		private readonly DownloaderService _downloaderService;
		private string[] _testInfo;

		public ModerationModule(
			DiscordSocketClient client,
			ModerationService mod,
			LevelTesting levelTesting,
			DataServices dataServices,
			TimerService timer,
			DownloaderService dlService)
		{
			_client = client;
			_timer = timer;
			_dataServices = dataServices;
			_levelTesting = levelTesting;
			_mod = mod;
			_downloaderService = dlService;
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
			[Summary("A template or the input for the interactive builder's current prompt.")] [Remainder]
			string input = null)
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
			string embedUrl = null;
			string footText = null;
			string authName = null;
			string footIconUrl = null;
			string embedImageUrl = null;

			List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();

			if (input != null)
			{
				//Reg ex match for {TAGNAME}
				Regex regex = new Regex("{([^}]*)}", RegexOptions.IgnoreCase);
				if (IsValidTag(input, regex))
				{
					string errors = null;

					/*
					 * While the string isn't Null, the beginning will always contain a tag like {title}
					 * The tag is removed and the following text is consumed until either the next tag is found
					 * or the end of the string is hit.
					 */
					while (input.Length > 0)
					{
						if (input.ToLower().StartsWith("{author name}"))
						{
							input = input.Substring(13);
							Match m = regex.Match(input);
							int textLength = m.ToString() != "" ? input.IndexOf(m.ToString()) : input.Length;

							authName = input.Substring(0, textLength);
							input = input.Substring(textLength);
						}

						if (input.ToLower().StartsWith("{thumbnail}"))
						{
							input = input.Substring(11);
							Match m = regex.Match(input);
							int textLength = m.ToString() != "" ? input.IndexOf(m.ToString()) : input.Length;

							embedThumbUrl = input.Substring(0, textLength);
							if (!Uri.IsWellFormedUriString(embedThumbUrl, UriKind.Absolute))
							{
								embedThumbUrl = null;
								errors += "THUMBNAIL URL NOT A PROPER URL. SET TO NULL\n";
							}

							input = input.Substring(textLength);
						}

						if (input.ToLower().StartsWith("{title}"))
						{
							input = input.Substring(7);
							Match m = regex.Match(input);
							int textLength = m.ToString() != "" ? input.IndexOf(m.ToString()) : input.Length;

							embedTitle = input.Substring(0, textLength);
							input = input.Substring(textLength);
						}

						if (input.ToLower().StartsWith("{url}"))
						{
							input = input.Substring(5);
							Match m = regex.Match(input);
							int textLength = m.ToString() != "" ? input.IndexOf(m.ToString()) : input.Length;

							embedUrl = input.Substring(0, textLength);
							if (!Uri.IsWellFormedUriString(embedUrl, UriKind.Absolute))
							{
								embedUrl = null;
								errors += "TITLE URL NOT A PROPER URL. SET TO NULL\n";
							}
;
							input = input.Substring(textLength);
						}

						if (input.ToLower().StartsWith("{color}"))
						{
							input = input.Substring(7);
							Match m = regex.Match(input);
							int textLength = m.ToString() != "" ? input.IndexOf(m.ToString()) : input.Length;

							string[] splitString = { null, null, null };
							splitString = input.Substring(0, textLength).Split(' ');
							try
							{
								var splitInts = splitString.Select(item => int.Parse(item)).ToArray();
								embedColor = new Color(splitInts[0], splitInts[1], splitInts[2]);
							}
							catch
							{
								errors += "INVALID RGB STRUCTURE. DEFUALT COLOR USED\n";
							}

							input = input.Substring(textLength);
						}

						if (input.ToLower().StartsWith("{description}"))
						{
							input = input.Substring(13);
							Match m = regex.Match(input);
							int textLength = m.ToString() != "" ? input.IndexOf(m.ToString()) : input.Length;

							embedDescription = input.Substring(0, textLength);
							input = input.Substring(textLength);
						}

						if (input.ToLower().StartsWith("{image}"))
						{
							input = input.Substring(7);
							Match m = regex.Match(input);
							int textLength = m.ToString() != "" ? input.IndexOf(m.ToString()) : input.Length;

							embedImageUrl = input.Substring(0, textLength);
							if (!Uri.IsWellFormedUriString(embedImageUrl, UriKind.Absolute))
							{
								embedImageUrl = null;
								errors += "IMAGE URL NOT A PROPER URL. SET TO NULL\n";
							}
							input = input.Substring(textLength);
						}

						if (input.ToLower().StartsWith("{footer text}"))
						{
							input = input.Substring(13);
							Match m = regex.Match(input);
							int textLength = m.ToString() != "" ? input.IndexOf(m.ToString()) : input.Length;

							footText = input.Substring(0, textLength);
							input = input.Substring(textLength);
						}

						if (input.ToLower().StartsWith("{field}"))
						{
							input = input.Substring(7);
							Match m = regex.Match(input);
							int textLength = m.ToString() != "" ? input.IndexOf(m.ToString()) : input.Length;

							string fieldTi = input.Substring(0, textLength);

							input = input.Substring(textLength + 2);

							//Match field text
							m = regex.Match(input);
							textLength = input.IndexOf(m.ToString());
							string fieldCo = input.Substring(0, textLength);

							input = input.Substring(textLength + 2);

							//Match field inline
							m = regex.Match(input);

							textLength = m.ToString() != "" ? input.IndexOf(m.ToString()) : input.Length;

							string tfStr = input.Substring(0, textLength);

							bool fieldIn = tfStr.ToLower().StartsWith("t");

							input = input.Substring(textLength);

							fieldBuilder.Add(new EmbedFieldBuilder { Name = fieldTi, Value = fieldCo, IsInline = fieldIn });
						}

						if (input.ToLower().StartsWith("{submit}"))
						{
							input = input.Substring(8);
							Match m = regex.Match(input);
							int textLength = m.ToString() != "" ? input.IndexOf(m.ToString()) : input.Length;

							quickSendChannel = input.Substring(0, textLength);
							input = input.Substring(textLength);
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
				IconUrl = footIconUrl
			};
			var builder = new EmbedBuilder()
			{
				Fields = fieldBuilder,
				Footer = footBuilder,
				Author = authBuilder,

				ImageUrl = embedImageUrl,
				Url = embedUrl,
				Title = embedTitle,
				ThumbnailUrl = embedThumbUrl,
				Color = embedColor,
				Description = embedDescription

			};
			bool submit = false;

			//We aren't quick sending the message. Enter wizard mode.
			if (quickSendChannel == null)
			{
				const string INSTRUCTIONS_STR = "Type one of the options. Do not include `>`. Auto timeout in 120 seconds:" +
											   "\n`Author Name` `Thumbnail` `Title` `URL` `Color` `Description` `Image` `Footer Text` `Field`" +
											   "\n`submit` to send it." + "\n`cancel` to abort.";
				var pic = await ReplyAsync("", false, embedLayout);
				var preview = await ReplyAsync("__**PREVIEW**__", false, builder);
				var instructions = await ReplyAsync(INSTRUCTIONS_STR);
				bool run = true;
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
							// ignored
						}

						bool valid = true;
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
										embedUrl = response.Content;
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
								string[] splitString = { null, null, null };
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
										embedImageUrl = response.Content;
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
										bool fInline = false;

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
								// ignored
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
							ImageUrl = embedImageUrl,
							Url = embedUrl,
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
							await instructions.ModifyAsync(x => { x.Content = INSTRUCTIONS_STR; });
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

			//Where do send the message, my dudes.
			if (submit)
			{
				var msg = await ReplyAsync("Send this to what channel?", false, builder);
				bool sent = false;
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
								await _dataServices.LogChannel.SendMessageAsync("", false, builder);
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

			//There was a {submit} just send it.
			if (quickSendChannel != null)
			{
				bool sent = false;
				foreach (SocketTextChannel s in Context.Guild.TextChannels)
				{
					if (s.Name.ToLower() == quickSendChannel)
					{
						await s.SendMessageAsync("", false, builder);
						await _dataServices.ChannelLog($"Embed created by {Context.User} was sent to {s.Name}!");
						await _dataServices.LogChannel.SendMessageAsync("", false, builder);
						sent = true;
					}
				}

				if (!sent)
				{
					await ReplyAndDeleteAsync("```CHANNEL NOT FOUND```", timeout: TimeSpan.FromSeconds(3));
				}
			}
		}

		// TODO: Move to ModerationServices.
		private bool IsValidTag(string inString, Regex regex)
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
			//Command Blacklist
			//TODO: Move to a config file.
			if (command.ToLower().Contains("rcon_password") || command.ToLower().Contains("exit"))
			{
				await ReplyAsync("```This command cannot be run from here. Ask TopHATTwaffle to do it.```");
				await _dataServices.ChannelLog($"{Context.User} was trying to run a blacklisted command", $"{command} was trying to be sent to {serverCode}");
				return;
			}

			//Get the server we are going to use
			var server = _dataServices.GetServer(serverCode);
			string reply = null;
			try
			{
				if (server != null)
					reply = await _dataServices.RconCommand(command, server);

				//Remove log messages from log
				string[] replyArray = reply.Split(
				new[] { "\r\n", "\r", "\n" },
				StringSplitOptions.None
				);
				reply = string.Join("\n", replyArray.Where(x => !x.Trim().StartsWith("L ")));
				reply = reply.Replace("discord.gg", "discord,gg").Replace(server.Password, "[PASSWORD HIDDEN]");

				if (reply.Length > 1880)
					reply = $"{reply.Substring(0, 1880)}\n[OUTPUT OMITTED...]";
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
				await ReplyAsync("```Cannot use this command unless a test is scheduled```");
				return;
			}
			string config = null;
			LevelTestingServer server = null;

			//Get the right server. If null, use the server in the event info. Else we'll use what was provided.
			server = _dataServices.GetServer(serverCode ?? _levelTesting.CurrentEventInfo[10].Substring(0, 3));

			if (_levelTesting.CurrentEventInfo[7].ToLower() == "competitive" || _levelTesting.CurrentEventInfo[7].ToLower() == "comp")
				config = _dataServices.CompConfig;
			else
				config = _dataServices.CasualConfig; //If not comp, casual.

			if (action.ToLower() == "pre")
			{
				_testInfo = _levelTesting.CurrentEventInfo; //Set the test info so we can use it when getting the demo back.
				var result = Regex.Match(_levelTesting.CurrentEventInfo[6], @"\d+$").Value;

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
				_testInfo = _levelTesting.CurrentEventInfo; //Set the test info so we can use it when getting the demo back.

				DateTime testTime = Convert.ToDateTime(_levelTesting.CurrentEventInfo[1]);
				string demoName = $"{testTime:MM_dd_yyyy}_{_levelTesting.CurrentEventInfo[2].Substring(0, _levelTesting.CurrentEventInfo[2].IndexOf(" "))}_{_levelTesting.CurrentEventInfo[7]}";

				await _dataServices.ChannelLog($"Playtest Start on {server.Name}", $"exec {config}" +
					$"\ntv_record {demoName}" +
					$"\nsay Playtest of {_levelTesting.CurrentEventInfo[2].Substring(0, _levelTesting.CurrentEventInfo[2].IndexOf(" "))} is now live! Be respectiful and GLHF!");

				await ReplyAsync($"```Playtest Start on {server.Name}" +
					$"\nexec {config}" +
					$"\ntv_record {demoName}```");

				await _dataServices.RconCommand($"exec {config}", server);
				await Task.Delay(3250);
				await _dataServices.RconCommand($"tv_record {demoName}", server);
				await Task.Delay(1000);
				await _dataServices.RconCommand($"say Demo started! {demoName}", server);
				await Task.Delay(1000);
				await _dataServices.RconCommand($"say Playtest of {_levelTesting.CurrentEventInfo[2].Substring(0, _levelTesting.CurrentEventInfo[2].IndexOf(" "))} is now live! Be respectful and GLHF!", server);
				await Task.Delay(1000);
				await _dataServices.RconCommand($"say Playtest of {_levelTesting.CurrentEventInfo[2].Substring(0, _levelTesting.CurrentEventInfo[2].IndexOf(" "))} is now live! Be respectful and GLHF!", server);
				await Task.Delay(1000);
				await _dataServices.RconCommand($"say Playtest of {_levelTesting.CurrentEventInfo[2].Substring(0, _levelTesting.CurrentEventInfo[2].IndexOf(" "))} is now live! Be respectful and GLHF!", server);
			}
			else if (action.ToLower() == "post")
			{
				await ReplyAsync($"```Playtest post started. Begin feedback!```");

				//Fire and forget. Start the post tasks and don't wait for them to complete.
				//Waiting can cause bot to drop from Discord.
				Task fireAndForget = PostTasks(server);

				await _dataServices.ChannelLog($"Playtest Post on {server.Name}", $"exec {_dataServices.PostConfig}" +
					$"\nsv_voiceenable 0" +
					$"\nGetting Demo and BSP file and moving into DropBox");
			}
			else if (action.ToLower() == "scramble" || action.ToLower() == "s")
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

		/// <summary>
		/// Post takes are here so we can just fire and forget them. Nothing relies on them so we can forget about waiting.
		/// </summary>
		/// <param name="server">Server Object</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		// TODO: Move to ModerationServices.
		private async Task PostTasks(LevelTestingServer server)
		{
			var authBuilder = new EmbedAuthorBuilder()
			{
				Name = $"Download Playtest Demo for {_testInfo[2]}",
				IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png",
			};

			var builder = new EmbedBuilder()
			{
				Author = authBuilder,
				Url = "http://demos.tophattwaffle.com",
				Title = "Download Here",
				ThumbnailUrl = _testInfo[4],
				Color = new Color(243, 128, 72),
				Description = $"You can get the demo for this playtest by clicking above!" +
				$"\n\n*Thanks for testing with us!*" +
				$"\n\n[Map Images]({_testInfo[5]}) | [Schedule a Playtest](https://www.tophattwaffle.com/playtesting/) | [View Testing Calendar](http://playtesting.tophattwaffle.com)"

			};

			var result = Regex.Match(_testInfo[6], @"\d+$").Value;
			await _dataServices.RconCommand($"host_workshop_map {result}", server);
			await Task.Delay(15000);
			await _dataServices.RconCommand($"exec {_dataServices.PostConfig}; say Please join the Level Testing voice channel for feedback!", server);
			await Task.Delay(3000);
			await _dataServices.RconCommand($"sv_voiceenable 0; say Please join the Level Testing voice channel for feedback!", server);
			await Task.Delay(3000);
			await _dataServices.RconCommand($"sv_cheats 1; say Please join the Level Testing voice channel for feedback!", server);
			await Task.Delay(3000);
			await _dataServices.RconCommand($"say Please join the Level Testing voice channel for feedback!", server);
			await Task.Delay(3000);
			await _dataServices.RconCommand($"say Please join the Level Testing voice channel for feedback!", server);

			// Starts downloading playtesting files in the background.
			_downloaderService.Start(_testInfo, server);

			var splitUser = _testInfo[3].Split('#');

			try
			{
				//Try to DM them the information to get their demos.
				await _client.GetUser(splitUser[0], splitUser[1]).SendMessageAsync("", false, builder);
			}
			catch
			{
				try
				{
					//If they don't accepts DMs, tag them in level testing.
					await _dataServices.TestingChannel.SendMessageAsync($"{_client.GetUser(splitUser[0], splitUser[1]).Mention} You can download your demo here:");
				}
				catch
				{
					//If it cannot get the name from the event info, nag them in level testing.
					await _dataServices.TestingChannel.SendMessageAsync($"Hey {_testInfo[3]}! Next time you submit for a playtest, make sure to include your full Discord name so I can mention you. You can download your demo here:");
				}
			}
			await _dataServices.TestingChannel.SendMessageAsync($"", false, builder);
		}

		[Command("shutdown")]
		[Summary("Shuts down the bot.")]
		[RequireContext(ContextType.Guild)]
		[RequireRole(Role.Moderators)]
		public async Task ShutdownAsync()
		{
			await Context.Message.DeleteAsync();
			await _dataServices.ChannelLog($"Shutting down! Invoked by {Context.Message.Author}");
			await Task.Delay(2000); //Without the delay the bot never logs the shutdown.
			Environment.Exit(0);
		}

		[Command("reload")]
		[Summary("Reloads data from settings files.")]
		[RequireContext(ContextType.Guild)]
		[RequireRole(Role.Moderators)]
		public async Task ReloadAsync()
		{
			await ReplyAsync("```Reloading Data!```");
			await _dataServices.ChannelLog($"{Context.User} reloaded bot data!");
			_dataServices.ReloadSettings();
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
			var lines = _dataServices.Config.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());
			var reply = string.Join(Environment.NewLine, lines);
			reply = reply.Replace((_dataServices.Config["botToken"]), "[TOKEN HIDDEN]");
			try
			{
				await Context.Message.Author.SendMessageAsync($"```{reply}```");
			}
			catch { }//Do nothing if we can't DM.
		}

		[Command("mute")]
		[Summary("Mutes a user.")]
		[Alias("m")]
		[RequireContext(ContextType.Guild)]
		[RequireRole(Role.Moderators)]
		public async Task MuteAsync(
			[Summary("The user to mute.")] SocketGuildUser user,
			[Summary("The duration, in minutes, of the mute.")]
			int durationMin = 5,
			[Summary("The reason for the mute.")] [Remainder]
			string reason = "No reason provided.")
		{
			await _mod.Mute(user, durationMin, Context.User, reason);
		}

		[Command("ClearReservations")]
		[Summary("Clears server reservations.")]
		[Remarks("If no server is specified, _all_ server reservations are cleared.")]
		[Alias("cr")]
		[RequireContext(ContextType.Guild)]
		[RequireRole(Role.Moderators)]
		public async Task ClearReservationsAsync(string serverStr = null)
		{
			if (serverStr == null)
				await _levelTesting.ClearServerReservations();
			else
				await _levelTesting.ClearServerReservations(serverStr);

			await ReplyAsync("", false, _levelTesting.DisplayServerReservations());
		}
	}
}
