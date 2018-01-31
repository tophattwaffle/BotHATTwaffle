using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BotHATTwaffle.Modules
{
	/// <summary>
	/// Contains utility functions for help commands.
	/// </summary>
	public interface IHelpService
	{
		/// <summary>
		/// Retrieves the names of the required contexts from a command's preconditions.
		/// </summary>
		/// <param name="preconditions">The command's preconditions.</param>
		/// <returns>An alphabetically sorted collection of the names of required contexts.</returns>
		IReadOnlyCollection<string> GetContexts(IEnumerable<PreconditionAttribute> preconditions);

		/// <summary>
		/// Retrieves the names of the required permissions from a command's preconditions.
		/// </summary>
		/// <param name="preconditions">The command's preconditions.</param>
		/// <returns>An alphabetically sorted collection of the names of required permissions.</returns>
		IReadOnlyCollection<string> GetPermissions(IEnumerable<PreconditionAttribute> preconditions);
	}

	/// <inheritdoc />
	public class HelpService : IHelpService
	{
		/// <inheritdoc />
		/// <remarks>
		/// <see cref="RequireContextAttribute"/> and <see cref="RequireNsfwAttribute"/> are considered contexts.
		/// </remarks>
		public IReadOnlyCollection<string> GetContexts(IEnumerable<PreconditionAttribute> preconditions) {
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

			return contexts.OrderBy(c => c).ToImmutableArray();
		}

		/// <inheritdoc />
		public IReadOnlyCollection<string> GetPermissions(IEnumerable<PreconditionAttribute> preconditions) {
			var permissions = new List<string>();

			foreach (PreconditionAttribute precondition in preconditions)
			{
				if (precondition is RequireUserPermissionAttribute attr)
					permissions.Add(attr.ChannelPermission?.ToString() ?? attr.GuildPermission.ToString());
			}

			return permissions.OrderBy(p => p).ToImmutableArray();
		}
	}

	public class HelpModule : ModuleBase<SocketCommandContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commandService;
		private readonly IHelpService _helpService;

		public HelpModule(DiscordSocketClient client, CommandService commandService, IHelpService helpService)
		{
			_client = client;
			_commandService = commandService;
			_helpService = helpService;
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
				              "list of commands available in the context in which this help command was invoked:"
			};

			// Sorts modules alphabetically and iterates them.
			foreach (ModuleInfo module in _commandService.Modules.OrderBy(m => m.Name))
			{
				var description = new StringBuilder();

				// Builds the help strings.
				foreach (CommandInfo cmd in module.Commands)
				{
					if ((await cmd.CheckPreconditionsAsync(Context)).IsSuccess)
						description.AppendLine($"{cmd.Name} - {cmd.Summary}");
				}

				if (description.Length != 0)
					embed.AddField(module.Name.Replace("Module", string.Empty), description.ToString());
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

		[Command("help")]
		[Summary("Provides help for a specific command.")]
		[Alias("h")]
		public async Task HelpAsync([Summary("The command for which to get help.")] string command)
		{
			// Deletes the invoking message if it's not a direct message.
			if (!Context.IsPrivate)
				await Context.Message.DeleteAsync();

			SearchResult result = _commandService.Search(Context, command);

			if (!result.IsSuccess)
			{
				await ReplyAsync($"No commands matching **{command}** were found.");
				return;
			}

			// Iterates command search results.
			for (var i = 0; i < result.Commands.Count; ++i)
			{
				CommandInfo cmd = result.Commands[i].Command;

				// If optional, name is italicised and default value is displayed.
				ImmutableArray<string> param = cmd.Parameters.Select(
						p => (p.IsOptional ? $"___{p.Name}___" : $"__{p.Name}__") +
							 (string.IsNullOrWhiteSpace(p.Summary) ? string.Empty : $" - {p.Summary}") +
						     (p.IsOptional ? $" Default: `{p.DefaultValue ?? "null"}`" : string.Empty))
					.ToImmutableArray();

				// Parameters for the usage string.
				string paramsUsage = string.Join(" ", cmd.Parameters.Select(p => p.IsOptional ? $"<{p.Name}>" : $"[{p.Name}]"));
				paramsUsage = string.IsNullOrWhiteSpace(paramsUsage) ? string.Empty : " " + paramsUsage;

				IReadOnlyCollection<string> contexts = _helpService.GetContexts(cmd.Preconditions);
				IReadOnlyCollection<string> permissions = _helpService.GetPermissions(cmd.Preconditions);

				// Creates the embed.
				var embed = new EmbedBuilder
				{
					Color = new Color(47, 111, 146),
					Title = $"\u2753 `{cmd.Name}` Help",
					Description = $"`{Program.COMMAND_PREFIX}{cmd.Name}{paramsUsage}`\n{cmd.Summary}"
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

				if (param.Any())
					embed.AddField("Parameters", string.Join("\n", param));

				if (contexts.Any())
					embed.AddInlineField("Contexts", string.Join("\n", contexts));

				if (permissions.Any())
					embed.AddInlineField("Permissions", string.Join("\n", permissions));

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
