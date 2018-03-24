using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Discord;
using Discord.WebSocket;

namespace BotHATTwaffle.Extensions
{
    /// <summary>
    /// Static class used for adding methods to <see cref="IMessage"/> and related types.
    /// </summary>
    public static class MessageExtensions
    {
        /// <summary>
        /// Retrieves distinct text channel mentions from a message.
        /// </summary>
        /// <param name="message">The message for which to retrieve mentions.</param>
        /// <returns>A distinct collection of mentioned text channels.</returns>
        public static IReadOnlyCollection<SocketTextChannel> GetChannelMentions(this SocketMessage message) =>
            message.MentionedChannels.Distinct().OfType<SocketTextChannel>().ToImmutableArray();
    }
}
