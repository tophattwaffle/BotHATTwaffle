using System;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using BotHATTwaffle.Models;
using BotHATTwaffle.Services;

using Discord.WebSocket;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using NodaTime;
using NodaTime.Text;

using Event = Google.Apis.Calendar.v3.Data.Event;

namespace BotHATTwaffle
{
	/// <summary>
	/// A service for interacting with Google Calendars.
	/// </summary>
	public class GoogleCalendar
	{
		private readonly CalendarService _calendar;
		private readonly DiscordSocketClient _client;
		private readonly DataServices _data;
		private readonly ServerService _server;
		private Event _previousEvent;

		public GoogleCalendar(DiscordSocketClient client, DataServices data, ServerService server)
		{
			_client = client;
			_data = data;
			_server = server;

			_calendar = new CalendarService(new BaseClientService.Initializer
			{
				HttpClientInitializer = GetCredential(),
				ApplicationName = "Google Calendar API .NET Quickstart"
			});

			TimeZone = DateTimeZoneProviders.Tzdb[_calendar.Settings.Get("timezone").Execute().Value];
		}

		/// <summary>
		/// The calendar's default time zone setting.
		/// </summary>
		public DateTimeZone TimeZone { get; }

		/// <summary>
		/// Tries to retrieves a playtesting event from the calendar.
		/// </summary>
		/// <returns>The retrieved event, or <c>null</c> if no event was found.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the event can't be serialised.</exception>
		public async Task<EventResult> GetEventAsync()
		{
			// Defines request and parameters.
			EventsResource.ListRequest request = _calendar.Events.List(_data.Config["testCalID"]);

			request.Q = " by "; // This will limit all search requests to ONLY get playtest events.
			request.TimeMin = DateTime.Now;
			request.ShowDeleted = false;
			request.SingleEvents = true;
			request.MaxResults = 1;
			request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

			// Executes the request for events and retrieves the first event in the resulting items.
			// TODO: Is it safe to change to SingleOrDefault given that MaxResults = 1?
			Event e = (await request.ExecuteAsync()).Items?.FirstOrDefault();

			if (e == null)
				return new EventResult { Status = Status.NotFound };

			Status? status = null;

			if (_previousEvent != null && _previousEvent.Id == e.Id)
			{
				if (_previousEvent.Updated == e.Updated)
					return new EventResult {CalEvent = e, Status = Status.Same};

				status = Status.Updated;
			}

			_previousEvent = e;

			if (!TrySerialiseEvent(e, out Models.Event output))
				return new EventResult { CalEvent = e, Status = status | Status.SerialisationError ?? Status.SerialisationError};

			return new EventResult { CalEvent = e, Event = output, Status = status | Status.Success ?? Status.Success};
		}

		/// <summary>
		/// Tries to serialises an event into a <see cref="Models.Event"/> instance.
		/// </summary>
		/// <param name="calEvent">The event to serialise.</param>
		/// <param name="output">The serialised object, or <c>null</c> if serialisation failed.</param>
		/// <returns><c>true</c> if serialisation is successful; <c>false</c> otherwise.</returns>
		private bool TrySerialiseEvent(Event calEvent, out Models.Event output)
		{
			ImmutableArray<string>? description = calEvent?.Description.Split('\n')
				.Select(line => line.Substring(line.IndexOf(':') + 1).Trim())
				.ToImmutableArray();

			// Null event or invalid description.
			if (description?.Length != 7)
			{
				output = null;
				return false;
			}

			string[] author = description.Value[0].Split('#');

			output = new Models.Event
			{
				StartTime = TryParseEventTime(calEvent.Start, out OffsetDateTime? start) ? start : null,
				Title = calEvent.Summary,
				Description = description.Value[6],
				Mode = Enum.TryParse(description.Value[4], true, out GameMode gameMode) ? gameMode : GameMode.Competitive,
				Server = _server.GetServer(calEvent.Location?.Split('.').FirstOrDefault()), // Splits to get the prefix.
				Author = _client.GetUser(author.ElementAtOrDefault(0), author.ElementAtOrDefault(1)), // Null if not found.
				AuthorName = description.Value[0],
				ModeratorName = description.Value[5],
				FeaturedImage = TryCreateUri(description.Value[1], out Uri image)? image : null,
				AlbumId = TryParseAlbumId(description.Value[2], out string id) ? id : null,
				WorkshopId = TryParseWorkshopId(description.Value[3], out string wId) ? wId : null
			};

			return true;
		}

		/// <summary>
		/// Tries to parse the starting time for an event.
		/// </summary>
		/// <param name="time">The starting time of the event.</param>
		/// <param name="output">An object representing the time in UTC, or <c>null</c> if the time failed to be parsed.</param>
		/// <returns><c>true</c> if parsing is successful; <c>false</c> otherwise.</returns>
		private bool TryParseEventTime(EventDateTime time, out OffsetDateTime? output)
		{
			output = null;

			if (time.DateTime.HasValue)
			{
				LocalDateTime lt = LocalDateTime.FromDateTime(time.DateTime.Value);
				ZonedDateTime zdt = lt.InZoneLeniently(TimeZone);
				output = zdt.ToOffsetDateTime().WithOffset(Offset.Zero);

				return true;
			}

			// Event is a whole-day event.
			LocalDatePattern pattern = LocalDatePattern.Iso;

			if (!pattern.Parse(time.Date).TryGetValue(LocalDate.MinIsoValue, out LocalDate date))
				return false;

			try
			{
				output = date.AtStartOfDayInZone(TimeZone).ToOffsetDateTime().WithOffset(Offset.Zero);

				return true;
			}
			catch (SkippedTimeException)
			{
				return false; // Extremely rare for this to occur (entire day skipped); not worth handling.
			}
		}

		/// <summary>
		/// Tries to parse the ID of an Imgur album from its URL.
		/// </summary>
		/// <param name="url">The album's URL.</param>
		/// <param name="id">The album's ID.</param>
		/// <returns><c>true</c> if parsing is successful; <c>false</c> otherwise.</returns>
		private static bool TryParseAlbumId(string url, out string id)
		{
			id = null;

			if (!TryCreateUri(url, out Uri uri))
				return false;

			if (uri.Host.Equals("imgur.com", StringComparison.OrdinalIgnoreCase) &&
				uri.Segments.Length == 3 &&
				uri.Segments[1].Equals("a/", StringComparison.OrdinalIgnoreCase))
			{
				id = uri.Segments[2].TrimEnd('/');
			}

			return !string.IsNullOrWhiteSpace(id);
		}

		/// <summary>
		/// Tries to parse the ID of a Steam workshop item from its URL.
		/// </summary>
		/// <param name="url">The workshop item's URL.</param>
		/// <param name="id">The workshop item's ID.</param>
		/// <returns><c>true</c> if parsing is successful; <c>false</c> otherwise.</returns>
		private static bool TryParseWorkshopId(string url, out string id)
		{
			id = null;

			if (!TryCreateUri(url, out Uri uri))
				return false;

			if (uri.Host.Equals("steamcommunity.com", StringComparison.OrdinalIgnoreCase) &&
				uri.Segments.Length == 3 &&
				uri.Segments[1].Equals("sharedfiledetails/", StringComparison.OrdinalIgnoreCase) &&
				uri.Segments[2].Equals("filedetails/", StringComparison.OrdinalIgnoreCase))
			{
				NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);

				if (query.AllKeys.Contains("id", StringComparer.OrdinalIgnoreCase))
					id = query["id"];
			}

			return !string.IsNullOrWhiteSpace(id);
		}

		/// <summary>
		/// Tries to create a <see cref="Uri"/> from an absolute URL string.
		/// </summary>
		/// <param name="url">The string from which to construct the Uri.</param>
		/// <param name="uri">The created Uri, or <c>null</c> if it failed to be created.</param>
		/// <returns><c>true</c> if the Uri was successfully created; <c>false</c> otherwise.</returns>
		private static bool TryCreateUri(string url, out Uri uri)
		{
			try
			{
				uri = new Uri(url, UriKind.Absolute);

				return true;
			}
			catch (Exception e) when (e is ArgumentNullException || e is UriFormatException)
			{
				uri = null;

				return false;
			}
		}

		/// <summary>
		/// Retrieves a credential for the Google Calendar API.
		/// </summary>
		/// <remarks>
		/// The secret is read from <c>client_secret.json</c> located in the executable's directory.
		/// </remarks>
		/// <remarks>
		/// The token is stored in a file in <c>/credentials/calendar-dotnet-quickstart.json</c> in
		/// <see cref="Environment.SpecialFolder.Personal"/>.
		/// </remarks>
		/// <returns>The retrieved OAuth 2.0 credential.</returns>
		private static UserCredential GetCredential()
		{
			using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
			{
				// TODO: Chage this to the executable's directory or make it configurable.
				string credPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
				credPath = Path.Combine(credPath, ".credentials/calendar-dotnet-quickstart.json");

				return GoogleWebAuthorizationBroker.AuthorizeAsync(
						GoogleClientSecrets.Load(stream).Secrets,
						new[] { CalendarService.Scope.CalendarReadonly },
						"user",
						CancellationToken.None,
						new FileDataStore(credPath, true))
					.Result;
			}
		}
	}
}
