using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Linq;
using System.Threading;


namespace BotHATTwaffle
{
	/// <summary>
	/// I'd love to document this code, but I copy and pasted it
	/// from the Google site and modded it for my needs.
	/// </summary>
    public class GoogleCalendar
    {
        static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        static readonly string ApplicationName = "Google Calendar API .NET Quickstart";
        static string calID;
	    readonly DataServices _dataServices;

        public GoogleCalendar(DataServices dataServices)
        {
            _dataServices = dataServices;

            calID = null;
            if (_dataServices.Config.ContainsKey("testCalID"))
                calID = (_dataServices.Config["testCalID"]);
        }


        public string[] GetEvents()
        {
            UserCredential credential;
            //HEADER, TIME, TITLE, DESCRIPTION
            string[] finalEvent = new string[11];
            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/calendar-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                //Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Calendar API service.
            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define parameters of request.
            EventsResource.ListRequest request = service.Events.List(calID);

			//This will limit all search requests to ONLY get playtest events.
            request.Q = " by ";

            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 1;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            Events events = request.Execute();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    string desc = eventItem.Description;
                    string when = eventItem.Start.DateTime.ToString();

                    if (String.IsNullOrEmpty(when))
                    {
                        when = eventItem.Start.Date;
                    }
                    //Console.WriteLine("{0} ({1}) {2}", eventItem.Summary, when, desc, );
                    try
                    {
                        string[] formattingHolder = desc.Split('\n');

                        finalEvent[0] = "BEGIN_EVENT";
                        finalEvent[1] = when;
                        finalEvent[2] = eventItem.Summary;
                        finalEvent[3] = formattingHolder[0];
                        finalEvent[4] = formattingHolder[1];
                        finalEvent[5] = formattingHolder[2];
                        finalEvent[6] = formattingHolder[3];
                        finalEvent[7] = formattingHolder[4];
                        finalEvent[8] = formattingHolder[5];
                        finalEvent[9] = formattingHolder[6];
                        finalEvent[10] = "This is a bunch : of dummy date to be stripped";


                        finalEvent = finalEvent.Select((line, index) => line.Substring(line.IndexOf(':') >= 0 && index > 2 ? line.IndexOf(':') + 2 : 0).Trim()).ToArray();
                        finalEvent[10] = eventItem.Location;

                        if (finalEvent[10] == null)
                            finalEvent[10] = "No Server Set";
                    }
                    catch(Exception e)
                    {
	                    this._dataServices.ChannelLog($"There is an issue with the description on the next playtest event." +
                            $"This is likely caused by HTML formatting on the description. \n{e}");
                        finalEvent[0] = "BAD_DESCRIPTION";
                        finalEvent[1] = null;
                        finalEvent[2] = null;
                        finalEvent[3] = null;
                        finalEvent[4] = null;
                        finalEvent[5] = null;
                        finalEvent[6] = null;
                        finalEvent[7] = null;
                        finalEvent[8] = null;
                        finalEvent[9] = null;
                        finalEvent[10] = null;
                    }
                }
            }
            else
            {
                //Console.WriteLine("NO_EVENT_FOUND");
                finalEvent[0] = "NO_EVENT_FOUND";
            }
            //Console.WriteLine($"\n\nI found the following playtest:");
            //Console.WriteLine(string.Join("\n\n",finalEvent));
            //Console.WriteLine($"\n\n");
            return finalEvent;
        }
    }
}