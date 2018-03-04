using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using BotHATTwaffle.Commands.Readers;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BotHATTwaffle.Services.Embed
{
	/// <summary>
	/// Builds an embed from a formatting string.
	/// </summary>
	public class QuickBuilder
	{
		private static readonly Regex _Regex = new Regex(
			@"(?:{field}(?<fname>.*?)\{\}(?<fvalue>.*?)(?:\{\}(?<finline>.*?))?(?=$|{(?:field|author name|author icon|author url|thumbnail|title|url|color|description|image|footer text|footer icon|submit)}))|{(?<action>author name|author icon|author url|thumbnail|title|url|color|description|image|footer text|footer icon|submit)}(?<value>.*?)(?=$|{(?:field|author name|author icon|author url|thumbnail|title|url|color|description|image|footer text|footer icon|submit)})",
			RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

		private readonly SocketCommandContext _context;
		private ICollection<Match> _matches;

		/// <summary>
		/// Constructs a quick embed builder at the given context.
		/// </summary>
		/// <param name="context">The context in which the embed will be built.</param>
		public QuickBuilder(SocketCommandContext context)
		{
			_context = context;
		}

		/// <summary>
		/// A newline-delimited string of errors encountered while building the embed.
		/// </summary>
		public string Errors { get; private set; }

		/// <summary>
		/// Builds an embed from a formatting string.
		/// <para>
		/// An action is in curly brackets and its value follows it. See <see cref="_Regex"/> and
		/// <see cref="BuilderAction.Actions"/> for supported actions.
		/// </para>
		/// </summary>
		/// <param name="input">The formatting string.</param>
		/// <returns>The embed built from the string.</returns>
		public Discord.Embed Build(string input)
		{
			_matches = _Regex.Matches(input).OfType<Match>().ToList();

			var embed = new EmbedBuilder
			{
				Author = new EmbedAuthorBuilder(),
				Footer = new EmbedFooterBuilder()
			};

			var errors = new StringBuilder();

			foreach (var (action, value) in ParseActions())
			{
				if (!action.Callback(value, embed))
					errors.AppendLine(action.Error);
			}

			embed.Fields = ParseFields().ToList();
			Errors = errors.ToString();

			return embed.Build();
		}

		/// <summary>
		/// Parses channels in the input string's matches.
		/// <para>Supports channel mentions, names, and IDs.</para>
		/// </summary>
		/// <returns>The parsed channels.</returns>
		public async Task<IReadOnlyCollection<SocketTextChannel>> ParseChannels()
		{
			IEnumerable<Task<SocketTextChannel>> tasks = _matches
				.Where(m => m.Groups["action"].Value.Equals("submit", StringComparison.OrdinalIgnoreCase))
				.Select(m => ChannelTypeReader<SocketTextChannel>.GetBestResultAsync(_context, m.Groups["value"].Value.Trim()));

			return (await Task.WhenAll(tasks)).ToImmutableArray();
		}

		/// <summary>
		/// Parses the actions in the input string's matches.
		/// </summary>
		/// <remarks>Found matches are removed from the matches collection.</remarks>
		/// <returns>The parsed actions and their values.</returns>
		private IEnumerable<(BuilderAction Action, string Value)> ParseActions()
		{
			foreach (Match match in _matches.ToImmutableArray())
			{
				if (!BuilderAction.Actions.TryGetValue(match.Groups["action"].Value, out BuilderAction action))
					continue;

				string value = match.Groups["value"].Value;

				if (string.IsNullOrWhiteSpace(value))
					continue;

				_matches.Remove(match);

				yield return (Action: action, Value: value);
			}
		}

		/// <summary>
		/// Parses the fields in the input string's matches.
		/// </summary>
		/// <remarks>Found matches are removed from the matches collection.</remarks>
		/// <returns>The field builders from the parsed fields.</returns>
		private IEnumerable<EmbedFieldBuilder> ParseFields()
		{
			foreach (Match match in _matches.ToImmutableArray())
			{
				string name = match.Groups["fname"].Value;
				string value = match.Groups["fvalue"].Value;

				if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
					continue;

				_matches.Remove(match);

				yield return new EmbedFieldBuilder
				{
					Name = name,
					Value = value,
					IsInline = match.Groups["finline"].Value.StartsWith("t", StringComparison.OrdinalIgnoreCase)
				};
			}
		}
	}
}
