using System;

using BotHATTwaffle.Modules.Json;

using Discord;
using Discord.WebSocket;

namespace BotHATTwaffle.Objects
{
	/// <summary>
	/// Stores data tied to a specific user.
	/// </summary>
	public class UserData
	{
		public SocketGuildUser User { get; set; }
		public DateTime MuteExpiration { get; set; }
		public DateTime ReservationExpiration { get; set; }
		public DateTime HandleJoinTime { get; set; }
		public Embed JoinMessage { get; set; }
		public JsonServer ReservedServer { get; set; }

		/// <summary>
		/// Determines if a user's mute has expired.
		/// </summary>
		/// <returns><c>true</c> if expired; <c>false</c> othwerise.</returns>
		public bool MuteExpired() => MuteExpiration < DateTime.Now;

		/// <summary>
		/// Determines if a user's server reservation has expired.
		/// </summary>
		/// <returns><c>true</c> if expired; <c>false</c> othwerise.</returns>
		public bool ReservationExpired() => ReservationExpiration < DateTime.Now;

		/// <summary>
		/// Determines if it's time to handle the new user joining the server.
		/// </summary>
		/// <returns><c>true</c> if the join can be handled now; <c>false</c> otherwise.</returns>
		public bool CanHandleJoin() => HandleJoinTime < DateTime.Now;
	}
}
