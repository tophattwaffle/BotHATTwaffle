using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

using BotHATTwaffle.Models;

using Discord;
using Discord.WebSocket;

using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;

namespace BotHATTwaffle.Services.Playtesting
{
	public class PlaytestingService
	{
		public List<UserData> UserData = new List<UserData>();
		private readonly DiscordSocketClient _client;
		private readonly DataService _dataService;
		public IUserMessage AnnounceMessage { get; set; }
		public EventCalendarService EventCalendarService;
		public string[] LastEventInfo;
		public string[] CurrentEventInfo;
		private bool _alertedHour = false;
		private bool _alertedStart = false;
		private bool _alertedTwenty = false;
		private bool _alertedFifteen = false;
		public bool CanReserve = true;
		private int _caltick = 0;
		private const string _ANNOUNCE_PATH = "announcement_id.txt";
		private bool _firstRun = true;
		private readonly Random _random;
		private IAlbum _featureAlbum;
		private bool _canRandomImage = false;
		private string _preEmbedImg;
		private int _failedToFetch = 0;
		private int _failedRetryCount = 10;

		public PlaytestingService(
			DiscordSocketClient client,
			DataService dataService,
			EventCalendarService calendarService,
			Random random,
			ITimerService timer)
		{
			_client = client;
			_dataService = dataService;
			_random = random;
			EventCalendarService = calendarService;
			CurrentEventInfo = EventCalendarService.GetEvents(); //Initial get of playtest.
			LastEventInfo = CurrentEventInfo; //Make sure array is same size for doing compares later.

			timer.AddCallback(Announce);
			timer.AddCallback(CheckServerReservations);
		}

		/// <summary>
		/// Reattaches to a previously sent Announcement message.
		/// Handles if the message is different from current playtest.
		/// If message was manually removed.
		/// </summary>
		private async void GetPreviousAnnounceAsync()
		{
			string[] announceData = File.ReadAllLines(_ANNOUNCE_PATH);

			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"Announcement Message File Found!\n{announceData[0]}\n{announceData[1]}");

			var announceId = Convert.ToUInt64(announceData[1]);

			if (announceData[0] == CurrentEventInfo[2]) //If saved title == current title
			{
				Console.WriteLine("Titles match! Attempting to reattach!");
				try
				{
					AnnounceMessage = await _dataService.AnnouncementChannel.GetMessageAsync(announceId) as IUserMessage;
					Console.WriteLine("SUCCESS!");
				}
				catch (Exception)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Unable to load previous announcement message");
				}
				await GetAlbum();
			}
			else
			{
				Console.WriteLine("Titles do not match. Attempting to delete old message!");
				try
				{
					AnnounceMessage = await _dataService.AnnouncementChannel.GetMessageAsync(announceId) as IUserMessage;
					await AnnounceMessage.DeleteAsync();
					AnnounceMessage = null;
					Console.WriteLine("Old message Deleted!\nForcing refresh to post new message.");
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
			if (_firstRun && File.Exists(_ANNOUNCE_PATH))
			{
				_firstRun = false;
				await LaunchSuppress();
				GetPreviousAnnounceAsync();
			}
			_caltick++;
			if (_dataService.CalUpdateTicks < _caltick)
			{
				_caltick = 0;
				CurrentEventInfo = EventCalendarService.GetEvents();

				if (AnnounceMessage == null) //No current message.
				{
					await PostAnnounce(await FormatPlaytestInformationAsync(CurrentEventInfo, false));
				}
				else if (CurrentEventInfo[2] == LastEventInfo[2]) //Title is same.
				{
					await UpdateAnnounce(await FormatPlaytestInformationAsync(CurrentEventInfo, false));
				}
				else //Title is different, scrub and rebuild
				{
					await RebuildAnnounce();
				}
			}

		}

		/// <summary>
		/// Auto suppresses any announcements on load if they had already been fired.
		/// This is typically used if the bot starts after an announcement has been fired.
		/// </summary>
		/// <returns></returns>
		private Task LaunchSuppress()
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("\nChecking if playtest announcements need to be suppressed...");
			DateTime time = Convert.ToDateTime(CurrentEventInfo[1]);

			TimeSpan singleHour = new TimeSpan(1, 0, 0);
			DateTime adjusted = DateTime.Now.Add(singleHour);
			int hourTimeCompare = DateTime.Compare(adjusted, time);
			if (hourTimeCompare > 0)
			{
				_alertedHour = true;
				Console.WriteLine("Suppressing Hour Announcement!");
			}

			TimeSpan twentyMinutes = new TimeSpan(0, 20, 0);
			DateTime twentyAdjusted = DateTime.Now.Add(twentyMinutes);
			int twentyTimeCompare = DateTime.Compare(twentyAdjusted, time);
			if (twentyTimeCompare > 0)
			{
				_alertedTwenty = true;
				Console.WriteLine("Suppressing Twenty Announcement!");
			}

			TimeSpan fifteenMinutes = new TimeSpan(0, 15, 0);
			DateTime fifteenAdjusted = DateTime.Now.Add(fifteenMinutes);
			int fifteenTimeCompare = DateTime.Compare(fifteenAdjusted, time);
			if (fifteenTimeCompare > 0)
			{
				_alertedFifteen = true;
				Console.WriteLine("Suppressing Fifteen Announcement!");
			}

			if (time.CompareTo(DateTime.Now) < 0)
			{
				_alertedStart = true;
				Console.WriteLine("Suppressing Started Announcement!");
			}

			Console.WriteLine();
			Console.ResetColor();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Gets an the IMGUR Album for the current playtest.
		/// </summary>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		private async Task GetAlbum()
		{
			_canRandomImage = true;
			try
			{
				string albumUrl = CurrentEventInfo[5];
				string albumId = albumUrl.Substring(albumUrl.IndexOf("/a/") + 3);
				var client = new ImgurClient(_dataService.ImgurApi);
				var endpoint = new AlbumEndpoint(client);

				_featureAlbum = await endpoint.GetAlbumAsync(albumId);

				string foundImages = null;

				foreach (var i in _featureAlbum.Images)
				{
					foundImages += $"{i.Link}\n";
				}

				await _dataService.ChannelLog("Getting Imgur Info from IMGUR API", $"Album URL: {albumUrl}" +
				$"\nAlbum ID: {albumId}" +
				$"\nClient Credits Remaining: {client.RateLimit.ClientRemaining} of {client.RateLimit.ClientLimit}" +
				$"\nImages Found: {foundImages}");
			}
			catch
			{
				await _dataService.ChannelLog($"Unable to get Imgur Album for Random Image for {CurrentEventInfo[2]}", "Falling back to the image in the cal event.");
				_canRandomImage = false;
			}
		}

		/// <summary>
		/// Posts a new announcement message
		/// </summary>
		/// <param name="embed">Message to post</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		private async Task PostAnnounce(Discord.Embed embed)
		{
			AnnounceMessage = await _dataService.AnnouncementChannel.SendMessageAsync("", false, embed) as IUserMessage;
			await GetAlbum();
			//If the file exists, just delete it so it can be remade with the new test info.
			if (File.Exists(_ANNOUNCE_PATH))
			{
				File.Delete(_ANNOUNCE_PATH);
			}

			//Create the text file containing the announce message
			if (!File.Exists(_ANNOUNCE_PATH))
			{
				using (StreamWriter sw = File.CreateText(_ANNOUNCE_PATH))
				{
					sw.WriteLine(CurrentEventInfo[2]);
					sw.WriteLine(AnnounceMessage.Id);
				}
			}
			await _dataService.ChannelLog("Posting Playtest Announcement", $"Posting Playtest for {CurrentEventInfo[2]}");
			LastEventInfo = CurrentEventInfo;
		}

		/// <summary>
		/// Attempts to update an announcement message
		/// </summary>
		/// <param name="embed">Message to update</param>
		/// <returns></returns>
		private async Task UpdateAnnounce(Discord.Embed embed)
		{
			try
			{
				await AnnounceMessage.ModifyAsync(x =>
				{
					x.Content = "";
					x.Embed = embed;
				});
				LastEventInfo = CurrentEventInfo;
				_failedToFetch = 0;
			}
			catch (Exception e)
			{
				//Failed to modify message.
				//Retry and if it still fails, it must be gone and safe to recreate.
				if (_failedToFetch >= _failedRetryCount)
				{
					Console.WriteLine($"{e.GetType()}: {e.Message}\n{e.StackTrace}\n");

					await _dataService.ChannelLog(
						"Attempted to modify announcement message, but I could not find it. Did someone delete it? Recreating a new message.");

					AnnounceMessage = null;
					_failedToFetch = 0;
				}
				else
				{
					_failedToFetch++;
					Console.WriteLine($"Failed to update Announcement Message. Attempting to modify {_failedRetryCount - _failedToFetch} more times be recreating message" +
									  $"\n{e.GetType()}: {e.Message}\n{e.StackTrace}\n");
				}
			}
		}

		/// <summary>
		/// A playtest event has passed. Reset for the next one
		/// </summary>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		private async Task RebuildAnnounce()
		{
			await _dataService.ChannelLog("Scrubbing Playtest Announcement", "Playtest is different from the last one. This is probably because" +
				"the last playtest is past. Let's tear it down and get the next test.");
			await AnnounceMessage.DeleteAsync();
			AnnounceMessage = null;
			LastEventInfo = CurrentEventInfo;

			//Reset announcement flags.
			_alertedHour = false;
			_alertedStart = false;
			_alertedTwenty = false;
			_alertedFifteen = false;
			CanReserve = true;

			await Announce();
		}

		/// <summary>
		/// Sets up the playtesting server for a playtest.
		/// </summary>
		/// <param name="serverStr">The full server information</param>
		/// <param name="type">What action to do. See inner comments</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		public async Task SetupServerAsync(string serverStr, bool type)
		{
			//type true = Change map
			//type false = set config
			var server = _dataService.GetServer(serverStr.Substring(0, 3));

			if (type) //Change map
			{
				var result = Regex.Match(CurrentEventInfo[6], @"\d+$").Value;
				await _dataService.RconCommand($"host_workshop_map {result}", server);
				await _dataService.ChannelLog("Changing Map on Test Server", $"'host_workshop_map {result}' on {server.Address}");
			}
			else //Set config and post about it
			{
				List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();
				var authBuilder = new EmbedAuthorBuilder()
				{
					Name = $"Setting up {server.Address} for {CurrentEventInfo[2]}",
					IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
				};

				fieldBuilder.Add(new EmbedFieldBuilder { Name = "Connect Info", Value = $"`connect {CurrentEventInfo[10]}`", IsInline = false });

				var builder = new EmbedBuilder()
				{
					Author = authBuilder,
					Fields = fieldBuilder,
					Title = $"Workshop Link",
					Url = CurrentEventInfo[6],
					ThumbnailUrl = CurrentEventInfo[4],
					Color = new Color(71, 126, 159),

					Description = $"**{server.Description}**\n\n{CurrentEventInfo[9]}"
				};
				await _dataService.TestingChannel.SendMessageAsync("", false, builder);
				await _dataService.ChannelLog("Setting postgame config", $"'exec postgame' on {server.Address}");
				await _dataService.RconCommand($"exec postgame", server);
			}
		}

		/// <summary>
		/// Formats the playtesting information into an embed message for sending in Announcements.
		/// Also preforms the tasks of:
		/// 1 Hour playtest alert
		/// 20 Minute Server Setup
		/// 15 Minute Server Setup
		/// Playtest Start Alert
		/// </summary>
		/// <param name="eventInfo">String array of playtest information</param>
		/// <param name="userCall">Did a user invoke this?</param>
		/// <returns></returns>
		public async Task<Discord.Embed> FormatPlaytestInformationAsync(string[] eventInfo, bool userCall)
		{
			/*	EVENT INFO ARRAY LAYOUT
			 *	0 EVENT HEADER. "BEGIN_EVENT" or "NO_EVENT_FOUND"
			 *	1 Time
			 *	2 Title
			 *	3 Creator
			 *	4 Featured Image
			 *	5 Map Images
			 *	6 Workshop Link
			 *	7 Game Mode
			 *	8 Moderator
			 *	9 Description
			 *	10 Location
			 */

			EmbedBuilder builder;
			EmbedAuthorBuilder authBuilder;
			List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();
			EmbedFooterBuilder footBuilder;

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
				//Switches time left to display "Started: HH MM ago!" instead of a count down.
				if (time.CompareTo(DateTime.Now) < 0)
				{
					timeLeftStr = $"Started: {timeLeft:h\'H \'m\'M\'} ago!";
					if (!userCall && !_alertedStart) //Prevents user calls for upcoming from sending alert message.
					{
						_alertedStart = true;

						//Enable mentions for playtester role
						await _dataService.PlayTesterRole.ModifyAsync(x =>
						{
							x.Mentionable = true;
						});

						//Display the map to be tested.
						await _dataService.TestingChannel.SendMessageAsync("", false, await FormatPlaytestInformationAsync(CurrentEventInfo, true));
						await _dataService.TestingChannel.SendMessageAsync($"{_dataService.PlayTesterRole.Mention}" +
						$"\n**Playtest starting now!** `connect {eventInfo[10]}`" +
						$"\n*Type `>playtester` to unsubscribe*");

						await _dataService.ChannelLog($"Posing start playtest message for {CurrentEventInfo[2]}");

						_alertedStart = true;

						//Disable mentions for playtester role
						await _dataService.PlayTesterRole.ModifyAsync(x =>
						{
							x.Mentionable = false;
						});
					}
				}
				else
					timeLeftStr = timeLeft.ToString("d'D 'h'H 'm'M'").TrimStart(' ', 'D', 'H', '0');

				//Let's check if we should be announcing a playtest. Easier to do it here since the variables are already computed.
				//1 hour announcement
				TimeSpan singleHour = new TimeSpan(1, 0, 0);
				DateTime adjusted = DateTime.Now.Add(singleHour);
				int timeCompare = DateTime.Compare(adjusted, time);
				if (timeCompare > 0 && !_alertedHour)
				{
					//Disables server reservations with >ps and clears existing ones
					CanReserve = false;
					await ClearServerReservations();

					_alertedHour = true;
					await _dataService.PlayTesterRole.ModifyAsync(x =>
					{
						x.Mentionable = true;
					});

					//Display the map to be tested.
					await _dataService.TestingChannel.SendMessageAsync("", false, await FormatPlaytestInformationAsync(CurrentEventInfo, true));

					await _dataService.TestingChannel.SendMessageAsync($"{_dataService.PlayTesterRole.Mention}" +
							$"\n**Playtest starting in 1 hour**" +
							$"\n*Type `>playtester` to unsubscribe*");

					await _dataService.ChannelLog($"Posing 1 hour playtest message for {CurrentEventInfo[2]}");

					await _dataService.PlayTesterRole.ModifyAsync(x =>
					{
						x.Mentionable = false;
					});
				}

				//Change map 20 minutes beforehand
				TimeSpan twentyMinutes = new TimeSpan(0, 20, 0);
				DateTime twentyAdjusted = DateTime.Now.Add(twentyMinutes);
				int twentyTimeCompare = DateTime.Compare(twentyAdjusted, time);
				if (twentyTimeCompare > 0 && !_alertedTwenty)
				{
					_alertedTwenty = true;
					await SetupServerAsync(eventInfo[10], true);
				}

				//Exec postgame config for people to mess around on the server
				TimeSpan fifteenMinutes = new TimeSpan(0, 15, 0);
				DateTime fifteenAdjusted = DateTime.Now.Add(fifteenMinutes);
				int fifteenTimeCompare = DateTime.Compare(fifteenAdjusted, time);
				if (fifteenTimeCompare > 0 && !_alertedFifteen)
				{
					_alertedFifteen = true;
					await SetupServerAsync(eventInfo[10], false);
				}

				//Try to use the user's avatar as the thumbnail
				string thumbUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png";
				var splitUser = CurrentEventInfo[3].Split('#');
				try
				{
					thumbUrl = _client.GetUser(splitUser[0], splitUser[1]).GetAvatarUrl();
				}
				catch { }

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
					IconUrl = _client.CurrentUser.GetAvatarUrl()
				};

				//If possible, use a random image from the imgur album.
				string embedImage = eventInfo[4];
				try
				{
					if (_canRandomImage)
					{
						//Make sure the next image is different from the previous one
						bool unique = false;
						while (!unique)
						{
							embedImage = _featureAlbum.Images.ToArray()[(_random.Next(0, _featureAlbum.ImagesCount))].Link;

							if (embedImage != _preEmbedImg)
							{
								_preEmbedImg = embedImage;
								unique = true;
							}
						}
					}

				}
				catch { }

				builder = new EmbedBuilder()
				{
					Author = authBuilder,
					Footer = footBuilder,
					Fields = fieldBuilder,

					Title = $"Workshop Link",
					Url = eventInfo[6],
					ImageUrl = embedImage,
					ThumbnailUrl = thumbUrl,
					Color = new Color(71, 126, 159),

					Description = $"{eventInfo[9]}\n*level is loaded on the server 15 minutes before the start time.*"
				};
			}
			//There isn't a playtest to display
			else
			{
				string announceDiag = null;
				if (eventInfo[0].Equals("BAD_DESCRIPTION"))
					announceDiag = "\n\n\nThere was an issue with the Google Calendar event. Someone tell TopHATTwaffle..." +
						"If you're seeing this, that means there is probably a test scheduled, but the description contains " +
						"HTML code so I cannot properly parse it. ReeeeeeEEEeeE";

				authBuilder = new EmbedAuthorBuilder()
				{
					Name = "No Playtests Found!",
					IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
				};

				footBuilder = new EmbedFooterBuilder()
				{
					Text = "https://www.tophattwaffle.com/playtesting/",
					IconUrl = _client.CurrentUser.GetAvatarUrl()
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

		/// <summary>
		/// Checks all the server reservations to see if they have expired or not.
		/// </summary>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		public async Task CheckServerReservations()
		{
			//Loop reservations and clear them if needed.
			foreach (UserData u in UserData.ToList())
			{
				//The server reservation has expired
				if (u.ReservationExpired())
				{
					var authBuilder = new EmbedAuthorBuilder()
					{
						Name = $"Hey there {u.User.Username}!",
						IconUrl = u.User.GetAvatarUrl(),
					};
					var footBuilder = new EmbedFooterBuilder()
					{
						Text = $"This is in beta, please let TopHATTwaffle know if you have issues.",
						IconUrl = _client.CurrentUser.GetAvatarUrl()
					};

					var builder = new EmbedBuilder()
					{
						Footer = footBuilder,
						Author = authBuilder,
						ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
						Color = new Color(243, 128, 72),

						Description = $"Your reservation on {u.ReservedServer.Description} has ended! You can stay on the server but you cannot send any more commands to it."
					};

					try //If we cannot send a DM to the user, just dump it into the testing channel and tag them.
					{
						await u.User.SendMessageAsync("", false, builder);
					}
					catch
					{
						await _dataService.TestingChannel.SendMessageAsync(u.User.Mention, false, builder);
					}

					await _dataService.ChannelLog($"{u.User}'s reservation on {u.ReservedServer.Address} has ended.");
					await _dataService.RconCommand($"sv_cheats 0;sv_password \"\";say Hey there {u.User.Username}! Your reservation on this server has ended!", u.ReservedServer);
					UserData.Remove(u);
					await Task.Delay(1000);
				}
			}
		}

		/// <summary>
		/// Adds a server reservation for public user
		/// </summary>
		/// <param name="inUser">Requesting User</param>
		/// <param name="inServerReleaseTime">Release Time</param>
		/// <param name="server">Reserved Server</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		public async Task AddServerReservation(SocketGuildUser inUser, DateTime inServerReleaseTime, Server server)
		{
			await _dataService.ChannelLog($"{inUser} reservation on {server.Address} has started.", $"Reservation expires at {inServerReleaseTime}");
			await _dataService.RconCommand($"say Hey everyone! {inUser.Username} has reserved this server!", server);
			UserData.Add(new UserData()
			{
				User = inUser,
				ReservedServer = server,
				ReservationExpiration = inServerReleaseTime
			});
		}

		/// <summary>
		/// Clears ALL server reservations.
		/// </summary>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		public async Task ClearServerReservations()
		{
			foreach (UserData u in UserData.ToList())
			{
				var authBuilder = new EmbedAuthorBuilder()
				{
					Name = $"Hey there {u.User.Username}!",
					IconUrl = u.User.GetAvatarUrl(),
				};
				var footBuilder = new EmbedFooterBuilder()
				{
					Text = $"This is in beta, please let TopHATTwaffle know if you have issues.",
					IconUrl = _client.CurrentUser.GetAvatarUrl()
				};

				var builder = new EmbedBuilder()
				{
					Footer = footBuilder,
					Author = authBuilder,
					ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
					Color = new Color(243, 128, 72),

					Description = $"Your reservation on server {u.ReservedServer.Description} has expired because all reservations were cleared." +
					$"This is likely due to a playtest starting soon, or a moderator cleared all reservations."
				};

				try
				{
					await u.User.SendMessageAsync("", false, builder);
				}
				catch
				{
					await _dataService.TestingChannel.SendMessageAsync(u.User.Mention, false, builder);
				}

				await _dataService.ChannelLog($"{u.User}'s reservation on {u.ReservedServer.Address} has ended.");
				await _dataService.RconCommand($"sv_cheats 0;sv_password \"\"", u.ReservedServer);
				UserData.Remove(u);
				await Task.Delay(1000);
			}
		}

		/// <summary>
		/// Clears only a specific reservation
		/// </summary>
		/// <param name="serverStr"></param>
		/// <returns></returns>
		public async Task ClearServerReservations(string serverStr)
		{
			var server = _dataService.GetServer(serverStr);

			if (server == null)
				return;

			foreach (UserData u in UserData.ToList())
			{
				if (u.ReservedServer == server)
				{
					var authBuilder = new EmbedAuthorBuilder()
					{
						Name = $"Hey there {u.User.Username}!",
						IconUrl = u.User.GetAvatarUrl(),
					};
					var footBuilder = new EmbedFooterBuilder()
					{
						Text = $"This is in beta, please let TopHATTwaffle know if you have issues.",
						IconUrl = _client.CurrentUser.GetAvatarUrl()
					};

					var builder = new EmbedBuilder()
					{
						Footer = footBuilder,
						Author = authBuilder,
						ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
						Color = new Color(243, 128, 72),

						Description = $"Your reservation on server {u.ReservedServer.Description} has expired because the reservation was cleared." +
						$"This is likely due to a playtest starting soon, a moderator cleared the reservation, or you released the reservation."
					};

					try
					{
						await u.User.SendMessageAsync("", false, builder);
					}
					catch
					{
						await _dataService.TestingChannel.SendMessageAsync(u.User.Mention, false, builder);
					}

					await _dataService.ChannelLog($"{u.User}'s reservation on {u.ReservedServer.Address} has ended.");
					await _dataService.RconCommand($"sv_cheats 0;sv_password \"\"", u.ReservedServer);
					UserData.Remove(u);
					await Task.Delay(1000);
				}
			}
		}

		/// <summary>
		/// Displays all of the servers that have reservations on them
		/// </summary>
		/// <returns>Embed with server reservations</returns>
		public Discord.Embed DisplayServerReservations()
		{
			//Loop reservations and clear them if needed.
			var authBuilder = new EmbedAuthorBuilder()
			{
				Name = $"Current Server Reservations",
				//IconUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
			};

			List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();

			//Add all of the servers to the field list
			foreach (UserData u in UserData.ToList())
			{
				TimeSpan timeLeft = u.ReservationExpiration.Subtract(DateTime.Now);
				fieldBuilder.Add(new EmbedFieldBuilder
				{
					Name = $"{u.ReservedServer.Address}",
					Value = $"User: `{u.User}`" +
				$"\nTime Left: {timeLeft:h\'H \'m\'M\'}",
					IsInline = false
				});
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

		/// <summary>
		/// Checks if a server is already reserved
		/// </summary>
		/// <param name="server">Server to check</param>
		/// <returns>True if server is open, false if reserved</returns>
		public bool IsServerOpen(Server server)
		{
			foreach (UserData u in UserData.ToList())
			{
				if (u.ReservedServer == server)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Toggles the bot's announce flags for firing timed alerts for playtests.
		/// </summary>
		/// <param name="type"></param>
		public void SuppressAnnounce(int type)
		{
			switch (type)
			{
				case 1:
					_alertedHour = !_alertedHour;
					break;
				case 2:
					_alertedTwenty = !_alertedTwenty;
					break;
				case 3:
					_alertedFifteen = !_alertedFifteen;
					break;
				case 4:
					_alertedStart = !_alertedStart;
					break;
				case 5:
					_alertedHour = true;
					_alertedTwenty = true;
					_alertedFifteen = true;
					_alertedStart = true;
					break;
			}
		}

		public string GetAnnounceFlags() => $"Alerted Hour: {_alertedHour}\nAlerted Twenty: " +
											$"{_alertedTwenty}\nAlerted Fifteen: {_alertedFifteen}\nAlerted Start: {_alertedStart}";
	}

}
