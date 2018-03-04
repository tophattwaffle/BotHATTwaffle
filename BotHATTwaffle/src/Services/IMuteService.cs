﻿using System.Threading.Tasks;

using Discord.WebSocket;

namespace BotHATTwaffle.Services
{
	/// <summary>
	/// Enables muting of users and keeps track of muted users.
	/// </summary>
	public interface IMuteService
	{
		/// <summary>
		/// Mutes a <paramref name="user"/> for a <paramref name="reason"/> and the given <paramref name="duration"/>.
		/// </summary>
		/// <param name="user">The user to mute.</param>
		/// <param name="duration">The duration, in minutes, of the mute.</param>
		/// <param name="mod">The user which issued the mute.</param>
		/// <param name="reason">The reason for the mute.</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		Task MuteAsync(SocketGuildUser user, double duration, SocketUser mod, string reason = "");
	}
}