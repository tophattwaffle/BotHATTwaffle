using System.Collections.Generic;

namespace BotHATTwaffle.Extensions
{
    /// <summary>
    /// Static class used for adding methods to <see cref="Dictionary{TKey,TValue}"/> and related types.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>Tries to add a key and value to a <paramref name="dictionary"/>. Fails if the key already exists.</summary>
        /// <typeparam name="TKey">The type of the dictionary's key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary's value.</typeparam>
        /// <param name="dictionary">The dictionary to which to add the key value pair.</param>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns><c>true</c> if the pair was successfully added; <c>false</c> if the key already exists.</returns>
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                return false;

            dictionary.Add(key, value);

            return true;
        }

        /// <summary>
        /// Deconstructs a <see cref="KeyValuePair{TKey,TValue}"/> into a tuple with a key and value.
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
    }
}
