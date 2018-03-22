using System.Linq;

using Discord;

namespace BotHATTwaffle.Extensions
{
	/// <summary>
	/// Static class used for adding methods to <see cref="EmbedBuilder"/>.
	/// </summary>
	public static class EmbedBuilderExtensions
	{
		/// <summary>
		/// Calculates the length of an embed builder's contents.
		/// </summary>
		/// <remarks>
		/// The length is the sum of the lengths of the title, author's name, description, footer's text, and fields' names and
		/// values.
		/// </remarks>
		/// <param name="embed">The embed builder for which to calculates the length.</param>
		/// <returns>The embed builder's length.</returns>
		public static int Length(this EmbedBuilder embed) =>
			(embed.Title?.Length ?? 0) +
			(embed.Author?.Name?.Length ?? 0) +
			(embed.Description?.Length ?? 0) +
			(embed.Footer?.Text?.Length ?? 0) +
			embed.Fields.Sum(f => f.Name.Length + f.Value.ToString().Length);
	}
}
