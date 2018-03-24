using System.Threading.Tasks;
using Discord.Commands;
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
		/// <param name="context">Command Context - used for channel and invoking user</param>
		/// <param name="reason">The reason for the mute.</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		Task MuteAsync(SocketGuildUser user, int duration, SocketCommandContext context, string reason = "");

		/// <summary>
		/// Unmutes a <paramref name="user"/>
		/// </summary>
		/// <param name="user">The user to unmute</param>
		/// <returns></returns>
		Task<bool> CallUnMuteAsync(SocketGuildUser user);
	}
}
