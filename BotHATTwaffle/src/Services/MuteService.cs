using System;
using System.Linq;
using System.Threading.Tasks;

using BotHATTwaffle.Extensions;
using BotHATTwaffle.Models;
using Discord;
using Discord.WebSocket;

namespace BotHATTwaffle.Services
{
    /// <inheritdoc />
    public class MuteService : IMuteService
    {
        private readonly DiscordSocketClient _client;
        private readonly DataService _data;

        public MuteService(DiscordSocketClient client, DataService data, ITimerService timer)
        {
            _data = data;
            _client = client;

            timer.AddCallback(CheckMutesAsync);
        }

        /// <inheritdoc />
        public async Task<bool> MuteAsync(
            SocketGuildUser user,
            SocketUser muter,
            long? duration = null,
            string reason = null)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (!await DataBaseUtil.AddMuteAsync(user, muter, now, duration, reason))
            {
                await _data.ChannelLog($"Failure Muting {user}", "User is already muted.");

                return false;
            }

            await user.AddRoleAsync(
                _data.MuteRole,
                new RequestOptions {AuditLogReason = $"{muter}: {reason}".Truncate(512, true)});

            #region Messages

            var message = "You have been muted ";
            var logMessage = "Duration: ";

            if (duration.HasValue)
            {
                string expiration = now.AddMinutes(duration.Value).ToString("yyyy-MM-ddTHH:mm:ssZ");

                message += $"for `{duration}` minute(s) (expires `{expiration}`)";
                logMessage += $"{duration}\nExpires: {expiration}\nMuter: {muter}";
            }
            else
            {
                message += "indefinitely";
                logMessage += $"indefinitely\nMuter: {muter}";
            }

            if (reason != null)
            {
                message += $" because:```{reason}```";
                logMessage += $"\nReason: {reason}";
            }
            else
                message += ".";

            try
            {
                // Tries to send a DM.
                await user.SendMessageAsync(message);
            }
            catch
            {
                // Mentions the author in the the general channel instead.
                await _data.GeneralChannel.SendMessageAsync($"Hey {user.Mention}!\n{message}");
            }

            await _data.ChannelLog($"Muted {user}", logMessage);

            #endregion

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> UnmuteAsync(SocketGuildUser user, SocketUser unmuter, string reason = null)
        {
            if (!await DataBaseUtil.ExpireMuteAsync(user.Id))
            {
                if (unmuter != _client.CurrentUser)
                    await _data.ChannelLog($"Failure Unmuting {user}", "No active mute found.");

                return false;
            }

            // No need to check if the user has the role.
            await user.RemoveRoleAsync(
                _data.MuteRole,
                new RequestOptions {AuditLogReason = $"{unmuter}: {reason}".Truncate(512, true)});

            #region Messages

            string message = "You have been unmuted" + (reason == null ? "!" : $" because:```{reason}```");

            try
            {
                // Tries to send a DM.
                await user.SendMessageAsync(message);
            }
            catch
            {
                // Mentions the author in the the general channel instead.
                await _data.GeneralChannel.SendMessageAsync($"Hey {user.Mention}!\n{message}");
            }

            await _data.ChannelLog($"Unmuted {user}", $"Unmuter: {unmuter}\n{reason}");

            #endregion

            return true;
        }

        /// <summary>
        /// Checks for expired or manually removed mutes and appropriately unmutes users.
        /// </summary>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private async Task CheckMutesAsync()
        {
            SocketGuild guild = _client.Guilds.FirstOrDefault();

            if (guild == null) return;

            foreach (Mute mute in await DataBaseUtil.GetActiveMutesAsync())
            {
                SocketGuildUser user = guild.GetUser(mute.UserId);

                if (user == null)
                {
					//TODO: Handle this better so it does not spam each mute cycle
                    //await _data.ChannelLog($"Failure Unmuting {mute.Username}", "User not found.");

                    continue;
                }

                if (mute.CheckExpired())
                {
                    await UnmuteAsync(user, _client.CurrentUser, "The mute expired.");
                    await Task.Delay(1000);

	                continue;
				}

	            if (!user.Roles.Contains(_data.MuteRole))
	            {
		            await user.AddRoleAsync(_data.MuteRole);
		            await Task.Delay(1000);
	            }
			}
        }
    }
}
