using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Discord.WebSocket;

namespace BotHATTwaffle
{
	/// <summary>
	/// Static Extensions class used for adding methods to existing libraries and classes.
	/// </summary>
	public static class Extensions
	{
		/// <summary> Adds given key and value to dictionary if the given key does not already exist in dictionary.</summary>
		/// <typeparam name="TKey">The type of the dictionary's key.</typeparam>
		/// <typeparam name="TValue">The type of the dictionary's value.</typeparam>
		/// <param name="dictionary">The dictionary to check.</param>
		/// <param name="key">The key to check for existence.</param>
		/// <param name="value">The value to add if the key is missing.</param>
		/// <returns><c>true</c> if the key was found; <c>false</c> otherwise</returns>
		public static bool AddKeyIfMissing<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if (dictionary.ContainsKey(key)) return true;

			dictionary.Add(key, value);

			return false;
		}

		/// <summary>
		/// Deconstructs a <see cref="KeyValuePair"/> into a tuple with a key and value.
		/// </summary>
		/// <typeparam name="TKey">The key's type.</typeparam>
		/// <typeparam name="TValue">The value's type.</typeparam>
		/// <param name="tuple">The pair to deconstruct.</param>
		/// <param name="key">The key in the pair.</param>
		/// <param name="value">The value in the pair.</param>
		public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> tuple, out TKey key, out TValue value)
		{
			key = tuple.Key;
			value = tuple.Value;
		}

		/// <summary>
		/// Check if a string contains a substring using the specified <see cref="StringComparison"/>.
		/// </summary>
		/// <param name="source">The string to search.</param>
		/// <param name="toCheck">The string to search for an occurence of.</param>
		/// <param name="comp">The comparison rules to use.</param>
		/// <returns>
		/// <c>true</c> if <paramref name="source"/> contains <paramref name="toCheck"/>; <c>false</c> othwerise.
		/// </returns>
		public static bool Contains(this string source, string toCheck, StringComparison comp) =>
			source?.IndexOf(toCheck, comp) >= 0;

		/// <summary>
		/// Retrieves distinct text channel mentions from a message.
		/// </summary>
		/// <param name="message">The message for which to retrieve mentions.</param>
		/// <returns>A distinct collection of mentioned text channels.</returns>
		public static IReadOnlyCollection<SocketTextChannel> GetChannelMentions(this SocketMessage message) =>
			message.MentionedChannels.Distinct().OfType<SocketTextChannel>().ToImmutableArray();

		/// <summary>
		/// Shortens a string to the specified length by cutting off the end.
		/// </summary>
		/// <remarks>If the string's length is less than the maximum length, the original string is returned.</remarks>
		/// <param name="input">The string to truncate.</param>
		/// <param name="maxLength">The maximum length the resulting string should be.</param>
		/// <returns>The resulting string.</returns>
		public static string Truncate(this string input, int maxLength) =>
			input.Length < maxLength ? input : input.Substring(0, maxLength);

		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) => source.Shuffle(new Random());

		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (rng == null)
				throw new ArgumentNullException(nameof(rng));

			return source.ShuffleIterator(rng);
		}

		private static IEnumerable<T> ShuffleIterator<T>(this IEnumerable<T> source, Random rng)
		{
			List<T> buffer = source.ToList();

			for (var i = 0; i < buffer.Count; i++)
			{
				int j = rng.Next(i, buffer.Count);
				yield return buffer[j];

				buffer[j] = buffer[i];
			}
		}
	}
}
