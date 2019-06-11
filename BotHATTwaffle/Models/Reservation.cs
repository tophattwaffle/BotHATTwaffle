using System;

using BotHATTwaffle.Objects.Json;

using Discord.WebSocket;

namespace BotHATTwaffle.Models
{
	public class Reservation
	{
		public SocketUser Reserver { get; set; }
		public LevelTestingServer Server { get; set; }
		public DateTime Expiration { get; set; }

		/// <summary>
		/// Determines the reservation has expired.
		/// </summary>
		/// <returns><c>true</c> if expired; <c>false</c> othwerise.</returns>
		public bool Expired() => Expiration < DateTime.Now;

		/// <summary>
		/// Determines the reservation can be extended.
		/// <para>A reservation can be extended if it has less the 30 minutes remaining.</para>
		/// </summary>
		/// <returns><c>true</c> if extendable; <c>false</c> othwerise.</returns>
		public bool Extendable() => Expiration.AddMinutes(-30) < DateTime.Now;

		/// <summary>
		/// Calculates the time remaining before the reseveration expires.
		/// </summary>
		/// <returns>The time remaining on the reservation.</returns>
		public TimeSpan GetTimeLeft() => Expiration.Subtract(DateTime.Now);

		/// <summary>
		/// Determines the reservation can be extended.
		/// </summary>
		/// <param name="minutes">The duration, in minutes, by which to extend the reservation.</param>
		/// <returns><c>true</c> if successfully extended; <c>false</c> othwerise.</returns>
		/// <seealso cref="Extendable"/>
		public bool Extend(double minutes = 30)
		{
			if (!Extendable()) return false;

			Expiration = Expiration.AddMinutes(minutes);

			return true;
		}
	}
}
