using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace BotHATTwaffle
{
	/// <summary>
	/// A service for interacting with Google Calendars.
	/// </summary>
	public class GoogleCalendar
	{
		private readonly CalendarService _calendar;
		private readonly DataServices _dataServices;

		public GoogleCalendar(DataServices dataServices)
		{
			_dataServices = dataServices;
			_calendar = new CalendarService(new BaseClientService.Initializer
			{
				HttpClientInitializer = GetCredential(),
				ApplicationName = "Google Calendar API .NET Quickstart"
			});
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
						new[] {CalendarService.Scope.CalendarReadonly},
						"user",
						CancellationToken.None,
						new FileDataStore(credPath, true))
					.Result;
			}
		}

		/// <summary>
		/// Retrieves a playtesting event from the calendar.
		/// </summary>
		/// <remarks>
		/// <list type="number">
		///	<item><description>
		///	Header; possible values: <c>BEGIN_EVENT</c>, <c>NO_EVENT_FOUND</c>, <c>BAD_DESCRIPTION</c>
		///	</description></item>
		/// <item><description>Starting time</description></item>
		/// <item><description>Title</description></item>
		/// <item><description>Creator</description></item>
		/// <item><description>Featured image link</description></item>
		/// <item><description>Map images link</description></item>
		/// <item><description>Workshop link</description></item>
		/// <item><description>Game mode</description></item>
		/// <item><description>Moderator</description></item>
		/// <item><description>Description</description></item>
		/// <item><description>Server</description></item>
		/// </list>
		/// </remarks>
		/// <returns>An array of the details of the retrieved event.</returns>
		public string[] GetEvents()
		{
			// TODO: Replace the array with an object.
			var finalEvent = new string[11];

			// Defines request and parameters.
			EventsResource.ListRequest request = _calendar.Events.List(_dataServices.Config["testCalID"]);

			request.Q = " by "; // This will limit all search requests to ONLY get playtest events.
			request.TimeMin = DateTime.Now;
			request.ShowDeleted = false;
			request.SingleEvents = true;
			request.MaxResults = 1;
			request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

			// Executes the request for events and retrieves the first event in the resulting items.
			// TODO: Is it safe to change to SingleOrDefault given that MaxResults = 1?
			Event eventItem = request.Execute().Items?.FirstOrDefault();

			if (eventItem == null)
			{
				finalEvent[0] = "NO_EVENT_FOUND";
				return finalEvent;
			}

			// Handles the event.
			try
			{
				// Splits description into lines and keeps only the part after the colon, if one exists.
				ImmutableArray<string> description = eventItem.Description.Split('\n')
					.Select(line => line.Substring(line.IndexOf(':') + 1).Trim())
					.ToImmutableArray();

				finalEvent[0] = "BEGIN_EVENT";
				finalEvent[1] = eventItem.Start.DateTime?.ToString() ?? eventItem.Start.Date; // Accounts for all-day events.
				finalEvent[2] = eventItem.Summary;
				finalEvent[3] = description.ElementAtOrDefault(0) ?? string.Empty;
				finalEvent[4] = description.ElementAtOrDefault(1) ?? string.Empty;
				finalEvent[5] = description.ElementAtOrDefault(2) ?? string.Empty;
				finalEvent[6] = description.ElementAtOrDefault(3) ?? string.Empty;
				finalEvent[7] = description.ElementAtOrDefault(4) ?? string.Empty;
				finalEvent[8] = description.ElementAtOrDefault(5) ?? string.Empty;
				finalEvent[9] = description.ElementAtOrDefault(6) ?? string.Empty;
				finalEvent[10] = eventItem.Location ?? "No Server Set";
			}
			catch (Exception e)
			{
				// TODO: Narrow the exception being caught.

				// TODO: Is this even needed now that the description is parsed more safely?
				_dataServices.ChannelLog(
					"There is an issue with the description on the next playtest event. This is likely caused by HTML " +
					$"formatting on the description.\n{e}");

				// TODO: Is nulling the elements necessary? Are they ever accessed before the first element is validated?
				finalEvent = Enumerable.Repeat<string>(null, 11).ToArray();
				finalEvent[0] = "BAD_DESCRIPTION";
			}

			return finalEvent;
		}
	}
}
