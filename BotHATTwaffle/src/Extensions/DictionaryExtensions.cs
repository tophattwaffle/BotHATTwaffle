using System.Collections.Generic;

namespace BotHATTwaffle.Extensions
{
    /// <summary>
    /// Static class used for adding methods to <see cref="Dictionary{TKey,TValue}"/> and related types.
    /// </summary>
    public static class DictionaryExtensions
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
            if (dictionary.ContainsKey(key))
                return true;

            dictionary.Add(key, value);

            return false;
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
