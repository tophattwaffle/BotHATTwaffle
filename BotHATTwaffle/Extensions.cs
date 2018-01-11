using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotHATTwaffle
{
	/// <summary> Static Extensions class used for adding methods to existing libraries and classes </summary>
	public static class Extensions
	{
		/// <summary> Adds given key and value to dictionary if the given key does not already exist in dictionary.</summary>
		/// <typeparam name="TKey">Dictionary Key Type.</typeparam>
		/// <typeparam name="TValue">Dictionary Value Type.</typeparam>
		/// <param name="dictionary"></param>
		/// <param name="key">Key to check if it exists.</param>
		/// <param name="valueIfMissing">Value to add if the key was missing.</param>
		/// <returns>True if key was found. False if missing value was used.</returns>
		public static bool AddKeyIfMissing<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue valueIfMissing)
		{
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, valueIfMissing);
				return false;
			}
			return true;
		}
	}
}
