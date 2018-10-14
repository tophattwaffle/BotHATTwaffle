using System.Collections.Generic;

namespace BotHATTwaffle.Extensions
{
    /// <summary>
    /// Static class used for adding methods to <see cref="IList{T}"/>.
    /// </summary>
    public static class IListExtensions
    {
        /// <summary>
        /// Removes an element from the collection at the given index and returns the removed element.
        /// </summary>
        /// <typeparam name="T">The type of the objects in the collection.</typeparam>
        /// <param name="source">The collection on which to perform the operation.</param>
        /// <param name="index">The index of the element to removed.</param>
        /// <returns>The removed element.</returns>
        public static T PopAt<T>(this IList<T> source, int index)
        {
            T element = source[index];
            source.RemoveAt(index);

            return element;
        }

        /// <summary>
        /// Removes the last element from the collection and returns it.
        /// </summary>
        /// <typeparam name="T">The type of the objects in the collection.</typeparam>
        /// <param name="source">The collection on which to perform the operation.</param>
        /// <returns>The removed element.</returns>
        public static T Pop<T>(this IList<T> source) => source.PopAt(source.Count - 1);
    }
}
