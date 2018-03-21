using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using BotHATTwaffle.Services;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BotHATTwaffle.Commands
{
	/// <summary>
	/// Contains commands which provide help and bot information to users of the bot.
	/// </summary>
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

		[Command("Help")]
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

			DataBaseUtil.AddCommand(Context.User.Id.ToString(), Context.User.ToString(), "Help",
				Context.Message.Content, DateTime.Now);
		}

		[Command("Help")]
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
			DataBaseUtil.AddCommand(Context.User.Id.ToString(), Context.User.ToString(), "Help",
				Context.Message.Content, DateTime.Now);
		}

		[Command("About")]
		[Summary("Displays information about the bot.")]
		public async Task AboutAsync()
		{
			DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime.ToUniversalTime();

			var embed = new EmbedBuilder
			{
				Author = new EmbedAuthorBuilder
				{
					Name = "About BotHATTwaffle",
					IconUrl = _client.Guilds.FirstOrDefault()?.IconUrl
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

			DataBaseUtil.AddCommand(Context.User.Id.ToString(), Context.User.ToString(), "About",
				Context.Message.Content, DateTime.Now);
		}
	}
}
