using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using BotHATTwaffle.Models;

using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;

using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;

using NodaTime;
using NodaTime.Extensions;

namespace BotHATTwaffle.Services
{
	public class AnnouncementService : IAnnouncementService
	{
		private const string _SOURCE_ICON_URL =
			"https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png";

		private readonly GoogleCalendar _calendar;
		private readonly DiscordSocketClient _client;
		private readonly ZonedClock _clock;
		private readonly DataServices _data;
		private readonly ImgurClient _imgur;
		private readonly Random _random;

		private bool _firstRun = true;
		private int _calTick;
		private const uint _MAX_RETRIES = 10;

		public AnnouncementService(
			GoogleCalendar calendar,
			DiscordSocketClient client,
			ZonedClock clock,
			DataServices data,
			Random random,
			ITimerService timer
			)
		{
			_calendar = calendar;
			_client = client;
			_clock = clock;
			_data = data;
			_imgur = new ImgurClient(_data.ImgurApi);
			_random = random;

			timer.AddHandler(Announce);
		}

		/// <summary>
		/// The most recent announcement.
		/// </summary>
		internal Announcement Announcement { get; private set; }

		/// <summary>
		/// Retrieves an event from the calendar sends an announcement message for it.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">Provides data for the <see cref="Timer.Elapsed"/> event.</param>
		private async void Announce(object sender, ElapsedEventArgs e)
		{
			#region Skip Cycles

			// Effectively adds extra delay to this timer event by making it skip cycles.
			++_calTick;

			if (_calTick <_data.CalUpdateTicks) return;

			_calTick = 0;

			#endregion

			#region Event Retrieval

			Event ev;

			try
			{
				ev = await _calendar.GetEventAsync();
			}
			catch (InvalidOperationException ex)
			{
				Console.WriteLine("Error retrieving the event.\n" + ex);

				return;
			}

			#endregion

			#region Previous Message Retrieval

			IUserMessage previous = null;

			if (_firstRun)
			{
				previous = await GetPreviousMessageAsync();
				_firstRun = false;
			}

			#endregion

			if (Announcement == null)
			{
				if (ev == null)
				{
					// No events are scheduled.
					Announcement = new Announcement {Embed = BuildEmpty()};

					if (previous?.Embeds?.FirstOrDefault()?.Title == "Click here to schedule your playtest!")
						Announcement.Message = previous; // Previous message was for "no scheduled events" too; use it.
				}
				else
				{
					// Found a scheduled event.
					Announcement = new Announcement {Event = ev, Embed = Build(ev), AlbumImages = await GetAlbumImages(ev)};

					if (previous?.Embeds?.FirstOrDefault()?.Title == ev.Title.Truncate(256))
						Announcement.Message = previous; // Titles match; use the previous message.
				}

				if (Announcement.Message != null)
					await UpdateAsync();
				else
				{
					if (previous != null)
						await previous.DeleteAsync(); // No match, but a previous message exists; delete it.

					Announcement.Message = await SendAsync(ev, Announcement.Embed);
				}
			} else if (ev == null && Announcement.Event == null || ev?.Title == Announcement.Event?.Title)
				await UpdateAsync(); // Same event; updates the existing message.
			else
			{
				await Announcement.Message.DeleteAsync();

				/**
				 * The previous announcement was for a scheduled event;
				 * the event has passed and there are no more scheduled events.
				 *
				 * Otherwise, the previous announcement indicated the lack of events,
				 * but now a scheduled event has been found
				 * OR the titles of the two events just don't match i.e. there's a new, different event.
				 */
				if (ev == null && Announcement.Event != null)
					Announcement = new Announcement {Embed = BuildEmpty()};
				else
					Announcement = new Announcement {Event = ev, Embed = Build(ev), AlbumImages = await GetAlbumImages(ev)};

				Announcement.Message = await SendAsync(ev, Announcement.Embed);
			}
		}

		/// <summary>
		/// Sends a new announcement message and saves its ID to a file.
		/// </summary>
		/// <param name="e">The announcement's event.</param>
		/// <param name="embed">The embed to send.</param>
		/// <returns>The sent message.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="embed"/> is null.</exception>
		private async Task<RestUserMessage> SendAsync(Event e, EmbedBuilder embed)
		{
			if (embed == null)
				throw new ArgumentNullException(nameof(embed));

			string title = e?.Title ?? embed.Title;

			await _data.ChannelLog("Sending Event Announcement", $"Announcing the event {title}.");
			RestUserMessage message = await _data.AnnouncementChannel.SendMessageAsync(string.Empty, false, embed.Build());
			await Announcement.Save(message.Id);

			return message;
		}

		/// <summary>
		/// Attempts to update the announcement message.
		/// <para>Makes <see cref="_MAX_RETRIES"/> retries before giving up and resetting.</para>
		/// </summary>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		private async Task UpdateAsync()
		{
			// Updates the time field.
			if (Announcement.Embed.Fields?.FirstOrDefault()?.Name == "Time Until Test")
			{
				Announcement.Embed.Fields[0].Value = Announcement.Event.StartTime.HasValue
					? GetTimeLeft(Announcement.Event.StartTime.Value)
					: "Error retrieving time.";
			}

			try
			{
				await Announcement.Message.ModifyAsync(a => a.Embed = Announcement.Embed.Build());
				Announcement.Retries = 0;
			}
			catch (HttpException e)
			{
				if (e.DiscordCode.HasValue && e.DiscordCode.Value != 10008) throw; // Not an unknown message exception.

				// Failed to modify the message. Retry and if it still fails, it must be gone and safe to recreate.
				if (Announcement.Retries == _MAX_RETRIES)
				{
					Announcement = null;
					await _data.ChannelLog(
						"The announcement couldn't be updated because the message couldn't be found; it will be recreated.");
				}
				else
				{
					++Announcement.Retries;
					Console.WriteLine(
						"Failed to update the announcement message. " +
						$"Attempting to update {_MAX_RETRIES - Announcement.Retries} more times before recreating the message.");
				}
			}
		}

		/// <summary>
		/// Creates an embed which displays the event's information.
		/// </summary>
		/// <param name="e">The event for which to build an embed.</param>
		/// <returns>The created embed.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="e"/> is null.</exception>
		private EmbedBuilder Build(Event e)
		{
			if (e == null)
				throw new ArgumentNullException(nameof(e));

			var embed = new EmbedBuilder
			{
				Title = "Workshop Link",
				Url = "https://steamcommunity.com/sharedfiles/filedetails/?id=" + e.WorkshopId,
				Color = new Color(71, 126, 159),
				Description = e.Description.Truncate(2048),
				ThumbnailUrl =
					e.Author?.GetAvatarUrl() ?? "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png"
			};

			embed.WithAuthor(e.Title.Truncate(256), _SOURCE_ICON_URL);
			embed.WithFooter("connect " + e.Server.Address, _client.CurrentUser.GetAvatarUrl());
			embed.AddInlineField("Time Until Test", e.StartTime.HasValue ? GetTimeLeft(e.StartTime.Value) : "Error retrieving time.");
			embed.AddInlineField("Where?", $"`{e.Server.Address}`");
			embed.AddInlineField("Author", e.Author?.Mention ?? e.AuthorName);
			embed.AddInlineField("Moderator", e.ModeratorName);
			embed.AddField(
				"Links",
				$"[Map Images](https://imgur.com/a/{e.AlbumId}) | " +
				"[Schedule a Playtest](https://www.tophattwaffle.com/playtesting/) | " +
				"[View Testing Calendar](http://playtesting.tophattwaffle.com)");
			embed.AddField("When?", e.StartTime.HasValue ? GetConvertedTimes(e.StartTime.Value) : "Error retrieving time.");

			return embed;
		}

		/// <summary>
		/// Creates an embed indicating the lack of scheduled events.
		/// </summary>
		/// <returns>The created embed.</returns>
		private EmbedBuilder BuildEmpty()
		{
			var embed = new EmbedBuilder
			{
				Title = "Click here to schedule your playtest!",
				Url = "https://www.tophattwaffle.com/playtesting/",
				ImageUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/header.png",
				// ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
				Color = new Color(214, 91, 47),
				Description =
					"Believe it or not, there aren't any playtests scheduled. Click the link above to schedule your own playtest!"
			};

			embed.WithAuthor("No Scheduled Events", _SOURCE_ICON_URL);
			embed.WithFooter("https://www.tophattwaffle.com/playtesting/", _client.CurrentUser.GetAvatarUrl());

			return embed;
		}

		/// <summary>
		/// Converts a time into several timezones and formats them as a string.
		/// <para>The result displays time in UTC, PST, CST, EST, and CET. DST is supported.</para>
		/// </summary>
		/// <param name="time">The time to convert.</param>
		/// <returns>The formatted string with the converted times.</returns>
		private static string GetConvertedTimes(OffsetDateTime time)
		{
			var sb = new StringBuilder(time.ToString("dddd dd MMMM HH:mm 'UTC'\n", CultureInfo.InvariantCulture));

			ZonedDateTime zoned = time.InFixedZone();
			ZonedDateTime pst = zoned.WithZone(DateTimeZoneProviders.Tzdb["America/Los_Angeles"]);
			ZonedDateTime cst = zoned.WithZone(DateTimeZoneProviders.Tzdb["America/Chicago"]);
			ZonedDateTime est = zoned.WithZone(DateTimeZoneProviders.Tzdb["America/New_York"]);
			ZonedDateTime cet = zoned.WithZone(DateTimeZoneProviders.Tzdb["Europe/Berlin"]);

			sb.Append(pst.ToString("ddd HH:mm", CultureInfo.InvariantCulture));
			sb.Append(pst.IsDaylightSavingTime() ? " PDT | " : " PST | ");
			sb.Append(cst.ToString("ddd HH:mm", CultureInfo.InvariantCulture));
			sb.Append(pst.IsDaylightSavingTime() ? " CDT | " : " CST | ");
			sb.Append(est.ToString("ddd HH:mm", CultureInfo.InvariantCulture));
			sb.Append(pst.IsDaylightSavingTime() ? " EDT | " : " EST | ");
			sb.Append(cet.ToString("ddd HH:mm", CultureInfo.InvariantCulture));
			sb.Append(pst.IsDaylightSavingTime() ? " CEST" : " CET");

			return sb.ToString();
		}

		/// <summary>
		/// Determines the time left until the event starts or how much time has elapsed since the event started.
		/// </summary>
		/// <param name="time">The event's starting time.</param>
		/// <returns>A formatted time string which displays the time left.</returns>
		private string GetTimeLeft(OffsetDateTime time)
		{
			Duration elapsed = _clock.GetCurrentOffsetDateTime() - time;

			return time.ToInstant() < _clock.GetCurrentInstant()
				? $"Started {elapsed:h'H' m'M'} ago!"
				: elapsed.ToString("d'D 'h'H 'm'M'", CultureInfo.InvariantCulture).TrimStart(' ', 'D', 'H', '0');
		}

		/// <summary>
		/// Retrieves an Imgur album from the given URL.
		/// </summary>
		/// <returns>A shuffled linked list of the retrieved album's images, or <c>null</c> if retrieval failed.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="e"/> is null.</exception>
		private async Task<LinkedList<IImage>> GetAlbumImages(Event e)
		{
			if (e == null)
				throw new ArgumentNullException(nameof(e));

			if (e.AlbumId == null) return null;

			try
			{
				var endpoint = new AlbumEndpoint(_imgur); // TODO: Does a new instance really need to be created every time?
				IAlbum album = await endpoint.GetAlbumAsync(e.AlbumId);
				// string images = string.Join("\n", album.Images.Select(i => i.Link));

				await _data.ChannelLog(
					$"Retrieved Imgur album for {e.Title}",
					$"Album ID: {e.AlbumId}\n" +
					$"Client Credits Remaining: {_imgur.RateLimit.ClientRemaining} of {_imgur.RateLimit.ClientLimit}");

				return new LinkedList<IImage>(album.Images.Shuffle(_random));
			}
			catch
			{
				// TODO: Narrow the exceptions being caught.
				await _data.ChannelLog(
					$"Unable to retrieve Imgur album for {e.Title}",
					"Falling back to the image in the calendar event.");

				return null;
			}
		}

		/// <summary>
		/// Retrieves a previously sent announcement message.
		/// </summary>
		/// <remarks>
		/// Reads a line from a file on disk which has the previous message's ID.
		/// </remarks>
		/// <returns>The previous message, or <c>null</c> if it couldn't be retrieved.</returns>
		private async Task<IUserMessage> GetPreviousMessageAsync()
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			ulong? id = await Announcement.Load();

			if (!id.HasValue)
				return null;

			try
			{
				var message = await _data.AnnouncementChannel.GetMessageAsync(id.Value) as IUserMessage;

				Console.WriteLine("Successfully found the previous announcement.");
				Console.ResetColor();

				return message;
			}
			catch (HttpException e)
			{
				// TODO: Narrow exceptions being caught.
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Unable to find the previous announcement message.");
				Console.ResetColor();

				return null;
			}
		}
	}
}
