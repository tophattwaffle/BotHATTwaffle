using System;
using System.Collections.Generic;
using System.Linq;

namespace BotHATTwaffle.Extensions
{
	/// <summary>
	/// Static class used for adding methods to <see cref="IEnumerable{T}"/>.
	/// </summary>
	public static class IEnumerableExtensions
	{
		// Shuffle: https://stackoverflow.com/a/1653204/5717792

		/// <summary>
		/// Peforms a Fisher-Yates-Durstenfeld shuffle on the collection.
		/// </summary>
		/// <typeparam name="T">The type of the objects in the collection.</typeparam>
		/// <param name="source">The collection on which to perform the shuffle.</param>
		/// <returns>The shuffled collection.</returns>
		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) => source.Shuffle(new Random());

		/// <summary>
		/// Peforms a Fisher-Yates-Durstenfeld shuffle on the collection using the specified <see cref="Random"/> instance.
		/// </summary>
		/// <typeparam name="T">The type of the objects in the collection.</typeparam>
		/// <param name="source">The collection on which to perform the shuffle.</param>
		/// <param name="rand">The <see cref="Random"/> instance to use to perform the shuffle.</param>
		/// <returns>The shuffled collection.</returns>
		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rand)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (rand == null)
				throw new ArgumentNullException(nameof(rand));

			return source.ShuffleIterator(rand);
		}

		/// <summary>
		/// Peforms a Fisher-Yates-Durstenfeld shuffle on the collection using the specified <see cref="Random"/> instance.
		/// </summary>
		/// <typeparam name="T">The type of the objects in the collection.</typeparam>
		/// <param name="source">The collection on which to perform the shuffle.</param>
		/// <param name="rand">The <see cref="Random"/> instance to use to perform the shuffle.</param>
		/// <returns>The shuffled collection.</returns>
		private static IEnumerable<T> ShuffleIterator<T>(this IEnumerable<T> source, Random rand)
		{
			List<T> buffer = source.ToList();

			for (var i = 0; i < buffer.Count; ++i)
			{
				int j = rand.Next(i, buffer.Count);
				yield return buffer[j];

				buffer[j] = buffer[i];
			}
		}
	}
}
