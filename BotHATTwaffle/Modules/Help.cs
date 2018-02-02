using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using BotHATTwaffle.Objects;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using ParameterInfo = Discord.Commands.ParameterInfo;

namespace BotHATTwaffle.Modules
{
	/// <summary>
	/// Contains utility functions for help commands.
	/// </summary>
	public interface IHelpService
	{
		/// <summary>
		/// Adds a field to an <paramref name="embed"/> containing the names and summaries of a <paramref name="module"/>'s
		/// commands.
		/// </summary>
		/// <param name="module">The field's module.</param>
		/// <param name="embed">The embed to which to add the field.</param>
		void AddModuleField(ModuleInfo module, ref EmbedBuilder embed);

		/// <summary>
		/// Retrieves the names of the required contexts from a command's preconditions.
		/// </summary>
		/// <param name="preconditions">The command's preconditions.</param>
		/// <returns>A newline-delimited string of the alphabetically sorted names of required contexts.</returns>
		string GetContexts(IEnumerable<PreconditionAttribute> preconditions);

		/// <summary>
		/// Formats the parameters of a command to be displayed in an embed.
		/// </summary>
		/// <param name="parameters">A newline-delimited string containing the command's parameters.</param>
		string GetParameters(IReadOnlyCollection<Discord.Commands.ParameterInfo> parameters);

		/// <summary>
		/// Retrieves the names of the required permissions from a command's preconditions.
		/// </summary>
		/// <param name="preconditions">The command's preconditions.</param>
		/// <returns>A newline-delimited string of the alphabetically sorted names of required permissions.</returns>
		string GetPermissions(IEnumerable<PreconditionAttribute> preconditions);

		/// <summary>
		/// Retrieves the names of the required roles from a command's preconditions.
		/// </summary>
		/// <param name="preconditions">The command's preconditions.</param>
		/// <param name="context">The command's context.</param>
		/// <returns>A newline-delimited string of the alphabetically sorted names of required roles.</returns>
		string GetRoles(IEnumerable<PreconditionAttribute> preconditions, ICommandContext context);

		/// <summary>
		/// Creates a usage string for a command.
		/// </summary>
		/// <param name="command">The command for which to create a usage string.</param>
		/// <returns>The formatted usage string.</returns>
		string GetUsage(CommandInfo command);
	}

	/// <inheritdoc />
	public class HelpService : IHelpService
	{
		/// <inheritdoc />
		/// <remarks>Commands are sorted alphabetically by name.</remarks>
		public void AddModuleField(ModuleInfo module, ref EmbedBuilder embed) {
			var commands = new StringBuilder();

			// Sorts commands alphabetically and builds the help strings.
			foreach (CommandInfo cmd in module.Commands.OrderBy(c => c.Name))
				commands.AppendLine($"`{cmd.Name}` - {cmd.Summary}");

			// Adds a field for the module if any commands for it were found. Removes 'Module' from the module's name.
			if (commands.Length != 0)
				embed.AddField(module.Name.Replace("Module", string.Empty), commands.ToString());
		}

		/// <inheritdoc />
		/// <remarks>
		/// <see cref="RequireContextAttribute"/> and <see cref="RequireNsfwAttribute"/> are considered contexts.
		/// </remarks>
		public string GetContexts(IEnumerable<PreconditionAttribute> preconditions) {
			var contexts = new List<string>();

			foreach (PreconditionAttribute precondition in preconditions)
			{
				switch (precondition)
				{
					case RequireContextAttribute attr:
						// Gets an enumerable of the set contexts.
						IEnumerable<Enum> setFlags =
							Enum.GetValues(typeof(ContextType)).Cast<Enum>().Where(m => attr.Contexts.HasFlag(m));
						contexts.AddRange(setFlags.Select(f => f.ToString())); // Adds each set context's name.

						break;
					case RequireNsfwAttribute _:
						contexts.Add("NSFW");

						break;
				}
			}

			return string.Join("\n", contexts.OrderBy(c => c));
		}

		/// <inheritdoc />
		/// <param name="parameters"></param>
		public string GetParameters(IReadOnlyCollection<ParameterInfo> parameters) {
			var param = new StringBuilder();

			foreach (ParameterInfo p in parameters)
			{
				param.Append(p.IsOptional ? $"___{p.Name}___" : $"__{p.Name}__"); // Italicises optional parameters.

				if (!string.IsNullOrWhiteSpace(p.Summary))
					param.Append($" - {p.Summary}");

				// Appends default value if parameter is optional.
				if (p.IsOptional)
					param.Append($" Default: `{p.DefaultValue ?? "null"}`");

				param.AppendLine();
			}

			return param.ToString();
		}

		/// <inheritdoc />
		public string GetPermissions(IEnumerable<PreconditionAttribute> preconditions) {
			var permissions = new List<string>();

			foreach (PreconditionAttribute precondition in preconditions)
			{
				if (precondition is RequireUserPermissionAttribute attr)
					permissions.Add(attr.ChannelPermission?.ToString() ?? attr.GuildPermission.ToString());
			}

			return string.Join("\n", permissions.OrderBy(p => p));
		}

		/// <inheritdoc />
		/// <remarks>
		/// Attempts to fetch role names from the attribute if the string constructor was used. Otherwise, if the context is a
		/// guild, converts IDs to names. If not in a guild, the name in <see cref="Role"/> is used. If it's not in the enum,
		/// the raw ID is displayed.
		/// </remarks>
		public string GetRoles(IEnumerable<PreconditionAttribute> preconditions, ICommandContext context)
		{
			var roles = new List<string>();

			foreach (PreconditionAttribute precondition in preconditions)
			{
				if (precondition is RequireRoleAttribute attr)
				{
					roles.AddRange(
						attr.RoleNames ??
						context.Guild?.Roles.Where(r => attr.RoleIds.Contains(r.Id)).Select(r => r.Name) ??
						attr.RoleIds.Select(id => ((Role)id).ToString()));
				}
			}

			return string.Join("\n", roles.OrderBy(r => r));
		}

		/// <inheritdoc />
		/// <remarks>
		/// Contains the command's prefix, name, and parameters. Normal parameters are surrounded in square brackets, optional
		/// ones in angled brackets.
		/// </remarks>
		public string GetUsage(CommandInfo command)
		{
			var usage = new StringBuilder(Program.COMMAND_PREFIX);
			usage.Append(command.Name);

			if (command.Parameters.Any())
			{
				usage.Append(" ");

				foreach (ParameterInfo p in command.Parameters)
					usage.Append(p.IsOptional ? $"<{p.Name}>" : $"[{p.Name}]");
			}

			return usage.ToString();
		}
	}

	public class HelpModule : ModuleBase<SocketCommandContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;
		private readonly IHelpService _help;

		public HelpModule(DiscordSocketClient client, CommandService commands, IHelpService help)
		{
			_client = client;
			_commands = commands;
			_help = help;
		}

		[Command("help")]
		[Summary("Displays this message.")]
		[Alias("h")]
		public async Task HelpAsync()
		{
			// Deletes the invoking message if it's not a direct message.
			if (!Context.IsPrivate)
				await Context.Message.DeleteAsync();

			var embed = new EmbedBuilder
			{
				Color = new Color(47, 111, 146),
				Title = "\u2753 Command Help",
				Description = $"A command can be invoked by prefixing its name with `{Program.COMMAND_PREFIX}`. To see usage " +
				              $"details for a command, use `{Program.COMMAND_PREFIX}help [command]`.\n\nThe following is a " +
				              "list of available commands:"
			};

			// Sorts modules alphabetically and adds a field for each one.
			foreach (ModuleInfo module in _commands.Modules.OrderBy(m => m.Name))
				_help.AddModuleField(module, ref embed);

			// Replies normally if a direct message fails.
			try
			{
				await Context.User.SendMessageAsync(string.Empty, false, embed.Build());
			}
			catch
			{
				await ReplyAsync(string.Empty, false, embed.Build());
			}
		}

		[Command("help")]
		[Summary("Provides help for a specific command.")]
		[Alias("h")]
		public async Task HelpAsync([Summary("The command for which to get help.")] string command)
		{
			// Deletes the invoking message if it's not a direct message.
			if (!Context.IsPrivate)
				await Context.Message.DeleteAsync();

			SearchResult result = _commands.Search(Context, command);

			if (!result.IsSuccess)
			{
				await ReplyAsync($"No commands matching **{command}** were found.");
				return;
			}

			// Iterates command search results.
			for (var i = 0; i < result.Commands.Count; ++i)
			{
				CommandInfo cmd = result.Commands[i].Command;

				string parameters = _help.GetParameters(cmd.Parameters);
				string contexts = _help.GetContexts(cmd.Preconditions);
				string permissions = _help.GetPermissions(cmd.Preconditions);
				string roles = _help.GetRoles(cmd.Preconditions, Context);

				// Creates the embed.
				var embed = new EmbedBuilder
				{
					Color = new Color(47, 111, 146),
					Title = $"\u2753 `{cmd.Name}` Help",
					Description = $"`{_help.GetUsage(cmd)}`\n{cmd.Summary}"
				};

				// Only includes result count if there's more than one.
				// Only includes message about optional parameters if the command has any.
				embed.WithFooter(
					(result.Commands.Count > 1 ? $"Result {i + 1}/{result.Commands.Count}." : string.Empty) +
					(cmd.Parameters.Any(p => p.IsOptional)
						? " Angle brackets and italics denote optional arguments/parameters."
						: string.Empty));

				if (!string.IsNullOrWhiteSpace(cmd.Remarks))
					embed.AddField("Details", cmd.Remarks);

				if (!string.IsNullOrWhiteSpace(parameters))
					embed.AddField("Parameters", parameters);

				if (!string.IsNullOrWhiteSpace(contexts))
					embed.AddInlineField("Contexts", contexts);

				if (!string.IsNullOrWhiteSpace(permissions))
					embed.AddInlineField("Permissions", permissions);

				if (!string.IsNullOrWhiteSpace(roles))
					embed.AddInlineField("Roles", roles);

				// Excludes the command's name from the aliases.
				if (cmd.Aliases.Count > 1)
				{
					embed.AddInlineField(
						"Aliases",
						string.Join("\n", cmd.Aliases.Where(a => !a.Equals(cmd.Name, StringComparison.OrdinalIgnoreCase))));
				}

				// Replies normally if a direct message fails.
				try
				{
					await Context.User.SendMessageAsync(string.Empty, false, embed.Build());
				}
				catch
				{
					await ReplyAsync(string.Empty, false, embed.Build());
				}
			}
		}

		[Command("about")]
		[Summary("Displays information about the bot.")]
		public async Task AboutAsync()
		{
			DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime.ToUniversalTime();

			var embed = new EmbedBuilder
			{
				Author = new EmbedAuthorBuilder
				{
					Name = "About BotHATTwaffle",
					IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png"
				},
				Url = "https://www.tophattwaffle.com/",
				ThumbnailUrl = _client.CurrentUser.GetAvatarUrl(),
				Color = new Color(130, 171, 206),
				Description = "BotHATTwaffle was started to centralize Source Engine Discord server functions that were " +
				              "fractured between multiple bots. This bot was my first attempt at a real C# program that other " +
				              "people would interact with.\n\nPlease let me know if you have any suggests or find bugs!"
			};

			embed.AddInlineField("Author", "[TopHATTwaffle](https://github.com/tophattwaffle)");
			embed.AddInlineField(
				"Contributors",
				"[BenBlodgi](https://github.com/BenVlodgi)\n" +
				"[Mark](https://github.com/MarkKoz)\n" +
				"[JimWood](https://github.com/JamesT-W)");
			embed.AddInlineField(
				"Build Date",
				$"{buildDate:yyyy-MM-ddTHH:mm:ssK}\n[Changelog](https://github.com/tophattwaffle/BotHATTwaffle/commits/master)");
			embed.AddInlineField(
				"Libraries",
				"[Discord.net V1.0.2](https://github.com/RogueException/Discord.Net)\n" +
				"[CoreRCON](https://github.com/ScottKaye/CoreRCON)\n" +
				"[Html Agility Pack](http://html-agility-pack.net/)\n" +
				"[Newtonsoft Json.NET](https://www.newtonsoft.com/json)\n" +
				"[SSH.NET](https://github.com/sshnet/SSH.NET/)\n" +
				"[FluentFTP](https://github.com/robinrodricks/FluentFTP)");

			embed.WithFooter("Build date");
			embed.WithTimestamp(buildDate);

			await ReplyAsync(string.Empty, false, embed.Build());
		}
	}
}
