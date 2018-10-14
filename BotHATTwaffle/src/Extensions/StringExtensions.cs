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
        /// <c>true</c> if <paramref name="source"/> contains <paramref name="toCheck"/>; <c>false</c> otherwise.
        /// </returns>
        public static bool Contains(this string source, string toCheck, StringComparison comp) =>
            source?.IndexOf(toCheck, comp) >= 0;

        /// <summary>
        /// Shortens a string to the specified length by cutting off the end.
        /// </summary>
        /// <remarks>If the string's length is less than the maximum length, the original string is returned.</remarks>
        /// <param name="input">The string to truncate.</param>
        /// <param name="maxLength">The maximum length the resulting string should be.</param>
        /// <param name="addEllipses"><c>true</c> to append ellipses to the string when truncated; <c>false</c> otherwise.</param>
        /// <returns>The resulting string.</returns>
        public static string Truncate(this string input, int maxLength, bool addEllipses = false)
        {
            return input.Length < maxLength ? input :
                addEllipses ? input.Substring(0, maxLength - 3) + "..." : input.Substring(0, maxLength);
        }
    }
}
