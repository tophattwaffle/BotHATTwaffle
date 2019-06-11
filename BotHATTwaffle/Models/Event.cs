using System;

using BotHATTwaffle.Objects.Json;

using Discord.WebSocket;

using NodaTime;

namespace BotHATTwaffle.Models
{
	public enum GameMode
	{
		Competitive,
		Casual
	}

	/// <summary>
	/// Represents a playtesting event.
	/// </summary>
	public class Event
	{
		public OffsetDateTime? StartTime { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public GameMode Mode { get; set; }
		public LevelTestingServer Server { get; set; }
		public SocketUser Author { get; set; }
		public string AuthorName { get; set; }
		// public SocketUser Moderator { get; set; } TODO: Could be possible if discriminator is added in event description.
		public string ModeratorName { get; set; }
		public Uri FeaturedImage { get; set; }
		public string AlbumId { get; set; }
		public string WorkshopId { get; set; }
	}
}
