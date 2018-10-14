using System;

using Discord;
using Discord.WebSocket;

namespace BotHATTwaffle.Models
{
    /// <summary>
    /// Stores data tied to a specific user.
    /// </summary>
    public class UserData
    {
        public SocketGuildUser User { get; set; }
        public DateTime ReservationExpiration { get; set; }
        public DateTime HandleJoinTime { get; set; }
        public Embed JoinMessage { get; set; }
        public Server ReservedServer { get; set; }

        /// <summary>
        /// Determines if a user's server reservation has expired.
        /// </summary>
        /// <returns><c>true</c> if expired; <c>false</c> otherwise.</returns>
        public bool ReservationExpired() => ReservationExpiration < DateTime.Now;

        /// <summary>
        /// Determines if a user's server reservation can be extended.
        /// </summary>
        /// <returns><c>true</c> if expired; <c>false</c> otherwise.</returns>
        public bool CanExtend() => ReservationExpiration.AddMinutes(-30) < DateTime.Now;

        /// <summary>
        /// Determines if it's time to handle the new user joining the server.
        /// </summary>
        /// <returns><c>true</c> if the join can be handled now; <c>false</c> otherwise.</returns>
        public bool CanHandleJoin() => HandleJoinTime < DateTime.Now;
    }
}
