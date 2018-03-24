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
		private readonly DiscordSocketClient _client;
		private bool firstRun = true;

		public MuteService(DataService data, ITimerService timer, DiscordSocketClient client)
		{
			_data = data;
			timer.AddCallback(CheckMutesAsync);
			_client = client;

			if(firstRun)
				LoadMutes();
		}

		private void LoadMutes()
		{
			firstRun = false;
			Console.WriteLine("Reading in mutes from database...");
			foreach (var mute in DataBaseUtil.GetActiveMutes())
			{
				var user = _client.Guilds.FirstOrDefault().GetUser((ulong)mute.snowflake);
				DateTimeOffset muteExp = DateTimeOffset.FromUnixTimeSeconds(mute.muted_time).AddMinutes(mute.mute_duration);
				_mutedUsers.Add(new UserData { User = user, MuteExpiration = muteExp});
				Console.WriteLine($"Mute added from database: {mute.username}\n");
			}
		}

		/// <inheritdoc />
		public async Task MuteAsync(SocketGuildUser user, int duration, SocketCommandContext context, string reason = "")
		{
			if (user.Roles.Contains(_data.ModRole))
			{
				await context.Channel.SendMessageAsync("",false,
					new EmbedBuilder().WithAuthor("Mods don't mute other Mods...")
						.WithDescription("Now you 2 need to learn to play nice and get along."));

				return;
			}

			//Check if the mute can be added to the DB.
			//If false means we cannot add due to them already having a mute.
			//If true added to active mute.
			if (DataBaseUtil.AddActiveMute(user, duration, context, reason, DateTimeOffset.UtcNow))
			{
				DateTimeOffset expiration = DateTime.UtcNow.AddMinutes(duration);

				_mutedUsers.Add(new UserData {User = user, MuteExpiration = expiration});
				await user.AddRoleAsync(_data.MuteRole);

				try
				{
					// Tries to send a DM.
					await user.SendMessageAsync($"You were muted for {duration} minute(s) because:```{reason}```");
				}
				catch
				{
					// Mentions the author in the the context channel instead.
					await context.Channel.SendMessageAsync(
						$"Hey {user.Mention}!\nYou were muted for {duration} minute(s) because:```{reason}```");
				}

				DataBaseUtil.AddMute(user, duration, context, reason, DateTimeOffset.Now);

				await _data.ChannelLog(
					$"{user} muted by {context.User}",
					$"Muted for {duration} minute(s) (expires {expiration}) because:\n{reason}");

				DataBaseUtil.AddCommand(context.User.Id, context.User.ToString(), "Mute",
					context.Message.Content, DateTimeOffset.Now);
			}
			else
			{
				await context.Channel.SendMessageAsync($"Cannot mute {user} as they already have an active mute.");
			}
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
		/// Forces through an unmute - does not care about unmute time
		/// </summary>
		/// <param name="user">User to unmute</param>
		/// <returns></returns>
		public async Task<bool> CallUnMuteAsync(SocketGuildUser user)
		{
			UserData found = null;

			foreach (var m in _mutedUsers)
			{
				if (user.Id == m.User.Id)
				{
					found = m;

					break;
				}
			}

			if (found != null)
			{
				await UnmuteAsync(found);
				return true;
			}
			else
				return false;
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

			DataBaseUtil.RemoveActiveMute(user.User);

			await user.User.SendMessageAsync("You have been unmuted!");
			await _data.ChannelLog($"{user.User} unmuted", reason);
		}
	}
}
