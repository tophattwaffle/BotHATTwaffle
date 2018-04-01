using System.Threading.Tasks;
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
        /// <param name="muter">The user which invoked the mute operation.</param>
        /// <param name="reason">The reason for the mute.</param>
        /// <returns><c>true</c> if successfully muted; <c>false</c> otherwise.</returns>
        Task<bool> MuteAsync(SocketGuildUser user, SocketUser muter, long? duration = null, string reason = null);

        /// <summary>
        /// Unmutes a user with an optional <paramref name="reason"/> (for logging).
        /// </summary>
        /// <param name="user">The user to unmute.</param>
        /// <param name="unmuter">The user which invoked the unmute operation.</param>
        /// <param name="reason">The reason for the unmute.</param>
        /// <returns><c>true</c> if successfully unmuted; <c>false</c> otherwise.</returns>
        Task<bool> UnmuteAsync(SocketGuildUser user, SocketUser unmuter, string reason = null);
    }
}
