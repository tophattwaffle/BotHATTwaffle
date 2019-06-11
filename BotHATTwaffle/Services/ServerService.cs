using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

using BotHATTwaffle.Models;
using BotHATTwaffle.Objects;
using BotHATTwaffle.Objects.Json;

using Discord;
using Discord.WebSocket;

namespace BotHATTwaffle.Services
{
	public class ServerService
	{
		public enum Status
		{
			Success = 0,
			ServerNotFound = 1,
			ServerReserved = 2,
			ReservationNotFound = 3,
			UserHasReservation = 4
		}

		public class Result
		{
			public Status Status { get; internal set; }
			public Reservation Reservation { get; internal set; }
		}

		private readonly Dictionary<ulong, Reservation> _reservations = new Dictionary<ulong, Reservation>();
		private readonly DataServices _data;

		public ServerService(DataServices data, ITimerService timer)
		{
			_data = data;
			timer.AddHandler(CheckReservationsAsync);
		}

		/// <summary>
		/// Adds a server reservation.
		/// </summary>
		/// <param name="reserver">The user reserving the server.</param>
		/// <param name="expiration">The insant in time at which the reservation will expire.</param>
		/// <param name="serverCode">The three-letter code which identifies the server to reserve.</param>
		/// <returns>A result indicating the success of this operation.</returns>
		public async Task<Result> ReserveAsync(SocketUser reserver, DateTime expiration, string serverCode)
		{
			if (!CanReserve(reserver.Id, out Reservation reservation))
				return new Result { Status = Status.UserHasReservation, Reservation = reservation };

			if (!CanReserve(serverCode, out reservation))
				return new Result {Status = Status.ServerReserved, Reservation = reservation};

			LevelTestingServer server = GetServer(serverCode);

			if (server == null)
				return new Result { Status = Status.ServerNotFound };

			reservation = new Reservation
			{
				Reserver = reserver,
				Server = server,
				Expiration = expiration
			};

			_reservations.Add(reserver.Id, reservation);

			await _data.ChannelLog($"{reserver} reservation on {server.Address} has started.", $"Reservation expires at {expiration}");
			await _data.RconCommand($"say Hey everyone! {reserver.Username} has reserved this server!", server);

			return new Result { Status = Status.Success, Reservation = reservation};
		}

		/// <summary>
		/// Clears all server reservations.
		/// </summary>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		public async Task ClearReservationsAsync()
		{
			foreach (Reservation reservation in _reservations.Values)
			{
				await ClearReservationAsync(reservation);
				await Task.Delay(1000);
			}
		}

		/// <summary>
		/// Clears a specific user's reservation.
		/// </summary>
		/// <param name="reserver">The ID of the user whose reservation to clear.</param>
		/// <returns>A result indicating the success of this operation.</returns>
		public async Task<Result> ClearReservationAsync(ulong reserver)
		{
			Reservation reservation = GetReservation(reserver);

			return await ClearReservationAsync(reservation);
		}

		/// <summary>
		/// Clears a reservation for a specific server.
		/// </summary>
		/// <param name="serverCode">
		/// The three-letter code which identifies the server for which to clear the reservation.
		/// </param>
		/// <returns>A result indicating the success of this operation.</returns>
		public async Task<Result> ClearReservationAsync(string serverCode)
		{
			Reservation reservation = GetReservation(serverCode);

			return await ClearReservationAsync(reservation);
		}

		/// <summary>
		/// Clears the given reservation.
		/// </summary>
		/// <param name="reservation">The reservation to clear.</param>
		/// <param name="expired"><c>true</c> if the reservation expired; <c>false</c> otherwise.</param>
		/// <returns>A result indicating the success of this operation.</returns>
		public async Task<Result> ClearReservationAsync(Reservation reservation, bool expired = false)
		{
			if (reservation == null)
				return new Result { Status = Status.ReservationNotFound };

			string avatar = reservation.Reserver.GetAvatarUrl();
			string description = expired
				? $"Your reservation on {reservation.Server.Description} has expired! You can stay on the server but you " +
				  "cannot send any more commands to it."
				: $"Your reservation on server {reservation.Server.Description} has ended because the reservation was " +
				  "cleared. This is likely due to a playtest starting soon, a moderator clearing the reservation, or you " +
				  "releasing the reservation.";
			string say = expired
				? $";say Hey there, {reservation.Reserver.Username}! Your reservation on this server has expired!"
				: string.Empty;

			var embed = new EmbedBuilder
			{
				ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
				Color = new Color(243, 128, 72),
				Description = description
			};

			embed.WithAuthor($"Hey there, {reservation.Reserver.Username}!", avatar);
			embed.WithFooter("This is in beta, please inform TopHATTwaffle of any issues.", avatar);

			// Tries to send a DM. Sends the message in the playtesting channel if the user's DMs are disabled.
			try
			{
				await reservation.Reserver.SendMessageAsync(string.Empty, false, embed.Build());
			}
			catch
			{
				await _data.TestingChannel.SendMessageAsync(reservation.Reserver.Mention, false, embed.Build());
			}

			_reservations.Remove(reservation.Reserver.Id);
			await _data.ChannelLog($"{reservation.Reserver}'s reservation for {reservation.Server.Address} has ended.");
			await _data.RconCommand("sv_cheats 0;sv_password \"\"" + say, reservation.Server);

			return new Result { Status = Status.Success, Reservation = reservation };
		}

		/// <summary>
		/// Retrieves all server reservations.
		/// </summary>
		/// <returns>All reservations.</returns>
		public IEnumerable<Reservation> GetReservations()
		{
			foreach (Reservation reservation in _reservations.Values)
				yield return reservation;
		}

		/// <summary>
		/// Gets the given user's reservation.
		/// </summary>
		/// <param name="reserver">The ID of the user whose reservation to retrieve.</param>
		/// <returns>The user's reservation or <c>null</c> if the user has no reservation.</returns>
		public Reservation GetReservation(ulong reserver) =>
			_reservations.TryGetValue(reserver, out Reservation reservation) ? reservation : null;

		/// <summary>
		/// Gets the reservation for the given server.
		/// </summary>
		/// <param name="serverCode">
		/// The three-letter code which identifies the server for which to retrieve the reservation.
		/// </param>
		/// <returns>The reservation associated with the server or <c>null</c> if no server is found.</returns>
		public Reservation GetReservation(string serverCode) =>
			_reservations.Values.FirstOrDefault(r => r.Server.Equals(GetServer(serverCode)));

		/// <summary>
		/// Retrieves all playtesting servers.
		/// </summary>
		/// <returns>The retrieved servers.</returns>
		public IEnumerable<LevelTestingServer> GetServers()
		{
			foreach (LevelTestingServer server in _data.Servers.Values)
				yield return server;
		}

		/// <summary>
		/// Retrieves a server object using an identifier string.
		/// </summary>
		/// <param name="serverCode">The three-letter code which identifies the server to retrieve.</param>
		/// <returns>The found server object, or <c>null</c> if no server was found.</returns>
		public LevelTestingServer GetServer(string serverCode)
		{
			if (serverCode == null) return null;

			return _data.Servers.TryGetValue(serverCode, out LevelTestingServer server) ? server : null;
		}

		public EmbedBuilder GetEmbedBase()
		{
			var embed = new EmbedBuilder
			{
				Color = new Color(243, 128, 72),
				ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png"
			};

			// embed.WithFooter("This is in beta; please inform TopHATTwaffle of any issues.");

			return embed;
		}

		/// <summary>
		/// Determines if a server can be reserved.
		/// </summary>
		/// <param name="serverCode">The server to check.</param>
		/// <param name="reservation">The reservation for the server, if one exists.</param>
		/// <returns><c>true</c> if the server can be reserved; <c>false</c> otherwise.</returns>
		private bool CanReserve(string serverCode, out Reservation reservation)
		{
			reservation =
				_reservations.Values.FirstOrDefault(r => r.Server.Name.Equals(serverCode, StringComparison.OrdinalIgnoreCase));

			return reservation != null;
		}

		/// <summary>
		/// Determines if a user can reserve any server.
		/// <para>A user can't reserve a server if they already have a reservation.</para>
		/// </summary>
		/// <param name="reserver">The ID of the user attempting to reserve a server.</param>
		/// <param name="reservation">The user's reservation, if it exists.</param>
		/// <returns><c>true</c> if the user can reserve a server; <c>false</c> otherwise.</returns>
		private bool CanReserve(ulong reserver, out Reservation reservation)
		{
			reservation = GetReservation(reserver);

			return reservation != null;
		}

		/// <summary>
		/// Checks for and removes any expired reservations.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">Provides data for the <see cref="Timer.Elapsed"/> event.</param>
		private async void CheckReservationsAsync(object sender, ElapsedEventArgs e)
		{
			foreach (Reservation reservation in _reservations.Values)
			{
				if (!reservation.Expired())
					continue;

				await ClearReservationAsync(reservation);
				await Task.Delay(1000);
			}
		}
	}
}
