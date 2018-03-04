using System;

namespace BotHATTwaffle.Extensions
{
	/// <summary>
	/// Static class used for adding methods to <see cref="string"/>.
	/// </summary>
	public static class StringExtensions
	{
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
		/// Shortens a string to the specified length by cutting off the end.
		/// </summary>
		/// <remarks>If the string's length is less than the maximum length, the original string is returned.</remarks>
		/// <param name="input">The string to truncate.</param>
		/// <param name="maxLength">The maximum length the resulting string should be.</param>
		/// <returns>The resulting string.</returns>
		public static string Truncate(this string input, int maxLength) =>
			input.Length < maxLength ? input : input.Substring(0, maxLength);
	}
}
