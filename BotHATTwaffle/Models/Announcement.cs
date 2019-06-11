using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading.Tasks;

using Discord;

using Imgur.API.Models;

namespace BotHATTwaffle.Models
{
	/// <summary>
	/// Represents a playtesting event announcement.
	/// </summary>
	public class Announcement
	{
		private const string _ANNOUNCE_PATH = @"announcement.txt";

		/// <summary>
		/// The event being announced.
		/// </summary>
		public Event Event { get; set; }

		/// <summary>
		/// The sent embed which displays the event's information.
		/// </summary>
		public EmbedBuilder Embed { get; set; }

		/// <summary>
		/// The sent message which contains the announcement's embed.
		/// </summary>
		public IUserMessage Message { get; set; }

		/// <summary>
		/// A collection of images of the map being tested.
		/// </summary>
		public LinkedList<IImage> AlbumImages { get; set; }

		/// <summary>
		/// The amount of times this announcement has failed to be sent.
		/// </summary>
		public uint Retries { get; set; }

		// TODO: Move loading & saving into the main configuration?
		/// <summary>
		/// Reads the saved message ID from the file <see cref="_ANNOUNCE_PATH"/>.
		/// </summary>
		/// <returns>The message ID, or <c>null</c> if a file IO or parsing error occurred.</returns>
		public static async Task<ulong?> Load()
		{
			if (!File.Exists(_ANNOUNCE_PATH))
			{
				Console.WriteLine($"{_ANNOUNCE_PATH} doesn't exist.");

				return null;
			}

			try
			{
				using (var sr = new StreamReader(_ANNOUNCE_PATH))
				{
					string line = await sr.ReadLineAsync();

					if (ulong.TryParse(line, out ulong id))
						return id;

					Console.WriteLine($"Error parsing '{line}' in {_ANNOUNCE_PATH} as an ID.");
				}
			}
			catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException || e is IOException)
			{
				Console.WriteLine($"Error reading {_ANNOUNCE_PATH}: " + e.Message);
			}

			return null;
		}

		/// <summary>
		/// Saves a message ID to the file <see cref="_ANNOUNCE_PATH"/>.
		/// </summary>
		/// <remarks>The file is created if it doesn't exist; previous values are overwritten.</remarks>
		/// <param name="id"></param>
		/// <returns><c>true</c> if the write operation succeeded; <c>false</c> otherwise.</returns>
		public static async Task<bool> Save(ulong id)
		{
			try
			{
				using (var sw = new StreamWriter(_ANNOUNCE_PATH, true))
				{
					await sw.WriteLineAsync(id.ToString());

					return true;
				}
			}
			catch (Exception e) when (e is UnauthorizedAccessException ||
									  e is DirectoryNotFoundException ||
									  e is IOException ||
									  e is SecurityException)
			{
				Console.WriteLine($"Error writing to {_ANNOUNCE_PATH}: " + e.Message);
			}

			return false;
		}
	}
}
