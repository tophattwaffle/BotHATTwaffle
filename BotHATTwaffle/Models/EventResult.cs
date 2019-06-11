using System;

namespace BotHATTwaffle.Models
{
	[Flags]
	public enum Status
	{
		Success,
		Same,
		Updated,
		NotFound,
		SerialisationError
	}

	/// <summary>
	/// Represents a result of an even retrieval operation on the calendar.
	/// </summary>
	public class EventResult
	{
		public Google.Apis.Calendar.v3.Data.Event CalEvent { get; set; }
		public Event Event { get; set; }
		public Status Status { get; set; }
	}
}
