using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

using BotHATTwaffle.Models;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BotHATTwaffle.Services
{
	/// <inheritdoc />
	public class MuteService : IMuteService
	{
		private readonly DataService _data;
		private readonly List<UserData> _mutedUsers = new List<UserData>();

		public MuteService(DataService data, ITimerService timer)
		{
			_data = data;
			timer.AddCallback(CheckMutesAsync);
		}

		/// <inheritdoc />
		public async Task MuteAsync(SocketGuildUser user, int duration, SocketCommandContext context, string reason = "")
		{
			DateTime expiration = DateTime.Now.AddMinutes(duration);

			_mutedUsers.Add(new UserData { User = user, MuteExpiration = expiration });
			await user.AddRoleAsync(_data.MuteRole);

			try
			{
				// Tries to send a DM.
				await user.SendMessageAsync($"You were muted for {duration} minute(s) because:```{reason}```");
			}
			catch
			{
				// Mentions the author in the the context channel instead.
				await context.Channel.SendMessageAsync($"Hey {user.Mention}!\nYou were muted for {duration} minute(s) because:```{reason}```");
			}

			await _data.ChannelLog(
				$"{user} muted by {context.User}",
				$"Muted for {duration} minute(s) (expires {expiration}) because:\n{reason}");
		}

		/// <summary>
		/// Checks for expired or manually removed mutes and appropriately unmutes users.
		/// </summary>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		private async Task CheckMutesAsync()
		{
			foreach (UserData user in _mutedUsers.ToList())
			{
				if (!user.User.Roles.Contains(_data.MuteRole))
				{
					await UnmuteAsync(user, $"The {_data.MuteRole.Name} role was manually removed.");

					continue;
				}

				if (!user.MuteExpired()) continue;

				await UnmuteAsync(user, "The mute expired.");
				await Task.Delay(1000);
			}
		}

		/// <summary>
		/// Unmutes a user with a <paramref name="reason"/> (for logging).
		/// </summary>
		/// <param name="user">The user to unmute.</param>
		/// <param name="reason">The reason for the unmute.</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		private async Task UnmuteAsync(UserData user, string reason = "")
		{
			if (!_mutedUsers.Remove(user))
				return; // Attempts to remove the user. Returns if not muted.

			await user.User.RemoveRoleAsync(_data.MuteRole); // No need to check if the user has the role.

			await user.User.SendMessageAsync("You have been unmuted!");
			await _data.ChannelLog($"{user.User} unmuted", reason);
		}
	}
}
