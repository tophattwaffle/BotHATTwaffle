using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Discord;

namespace BotHATTwaffle.Services.Embed
{
	/// <summary>
	/// Represents an action an embed builder can perform.
	/// </summary>
	internal class BuilderAction
	{
		#region Actions

		public static IReadOnlyDictionary<string, BuilderAction> Actions =
			new Dictionary<string, BuilderAction>(StringComparer.OrdinalIgnoreCase)
		{
			{
				"Author Name",
				new BuilderAction
				{
					Instructions = "Enter the author name:",
					Callback = (input, embed) =>
					{
						embed.Author.Name = input;
						return true;
					}
				}
			},
			{
				"Author Icon",
				new BuilderAction
				{
					Instructions = "Enter the author icon URL:",
					Error = "Invalid author icon URL.",
					Callback = (input, embed) =>
					{
						if (!Uri.IsWellFormedUriString(input, UriKind.Absolute)) return false;

						embed.Author.IconUrl = input;
						return true;
					}
				}
			},
			{
				"Author URL",
				new BuilderAction
				{
					Instructions = "Enter the author URL:",
					Error = "Invalid author URL.",
					Callback = (input, embed) =>
					{
						if (!Uri.IsWellFormedUriString(input, UriKind.Absolute)) return false;

						embed.Author.Url = input;
						return true;
					}
				}
			},
			{
				"Thumbnail",
				new BuilderAction
				{
					Instructions = "Enter the thumbnail URL:",
					Error = "Invalid thumbnail URL.",
					Callback = (input, embed) =>
					{
						if (!Uri.IsWellFormedUriString(input, UriKind.Absolute)) return false;

						embed.ThumbnailUrl = input;
						return true;
					}
				}
			},
			{
				"Title",
				new BuilderAction
				{
					Instructions = "Enter the title:",
					Callback = (input, embed) =>
					{
						embed.Title = input;
						return true;
					}
				}
			},
			{
				"URL",
				new BuilderAction
				{
					Instructions = "Enter the title URL:",
					Error = "Invalid title URL.",
					Callback = (input, embed) =>
					{
						if (!Uri.IsWellFormedUriString(input, UriKind.Absolute)) return false;

						embed.Url = input;
						return true;
					}
				}
			},
			{
				"Color",
				new BuilderAction
				{
					Instructions = "Enter the color in the form of `R G B` (e.g. `250 120 50`):",
					Error = "Invalid RGB format.",
					Callback = (input, embed) =>
					{
						string[] split = input.Split(' ');
						var i = 0;

						// Tries to parse each string as an int into the temporary variable i. Selects i and creates an array.
						ImmutableArray<int> rgb = split.Where(c => int.TryParse(c, out i)).Select(_ => i).ToImmutableArray();

						if (rgb.Length < 3) return false;

						embed.Color = new Color(rgb[0], rgb[1], rgb[2]);
						return true;
					}
				}
			},
			{
				"Description",
				new BuilderAction
				{
					Instructions = "Enter the description:",
					Callback = (input, embed) =>
					{
						embed.Description = input;
						return true;
					}
				}
			},
			{
				"Image",
				new BuilderAction
				{
					Instructions = "Enter the image URL:",
					Error = "Invalid image URL.",
					Callback = (input, embed) =>
					{
						if (!Uri.IsWellFormedUriString(input, UriKind.Absolute)) return false;

						embed.ImageUrl = input;
						return true;
					}
				}
			},
			{
				"Footer Text",
				new BuilderAction
				{
					Instructions = "Enter the footer text:",
					Callback = (input, embed) =>
					{
						embed.Footer.Text = input;
						return true;
					}
				}
			},
			{
				"Footer Icon",
				new BuilderAction
				{
					Instructions = "Enter the footer icon URL:",
					Error = "Invalid footer icon URL.",
					Callback = (input, embed) =>
					{
						if (!Uri.IsWellFormedUriString(input, UriKind.Absolute)) return false;

						embed.Footer.IconUrl = input;
						return true;
					}
				}
			}
		};

		#endregion

		/// <summary>
		/// The function to be called when this action is processed.
		/// </summary>
		public Func<string, EmbedBuilder, bool> Callback { get; set; }

		/// <summary>
		/// The error message to display when the action's value is invalid.
		/// </summary>
		public string Error { get; set; }

		/// <summary>
		/// The instruction message to display when prompting for a value.
		/// </summary>
		public string Instructions { get; set; }
	}
}
