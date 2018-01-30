using System;
using System.Collections.Generic;
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
	public class HelpModule : ModuleBase<SocketCommandContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _service;

		public HelpModule(DiscordSocketClient client, CommandService service)
		{
			_client = client;
			_service = service;
		}

		[Command("help")]
		[Summary("`>help` Displays this message.")]
		[Alias("h")]
		public async Task HelpAsync()
		{
			// Deletes the invoking message if it's not a direct message.
			if (!Context.IsPrivate)
				await Context.Message.DeleteAsync();

			var embed = new EmbedBuilder
			{
				Color = new Color(47, 111, 146),
				Description = "These are the available commands:"
			};

			foreach (ModuleInfo module in _service.Modules)
			{
				var description = new StringBuilder();

				// Builds the help strings.
				foreach (CommandInfo cmd in module.Commands)
				{
					if ((await cmd.CheckPreconditionsAsync(Context)).IsSuccess)
						description.AppendLine($"{cmd.Name} - {cmd.Summary}");
				}

				if (description.Length != 0)
					embed.AddField(module.Name, description.ToString());
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
		[Summary("`>help [command]` Provides help for a specific command.")]
		[Alias("h")]
		public async Task HelpAsync(string command)
		{
			// Deletes the invoking message if it's not a direct message.
			if (!Context.IsPrivate)
				await Context.Message.DeleteAsync();

			SearchResult result = _service.Search(Context, command);

			if (!result.IsSuccess)
			{
				await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
				return;
			}

			var builder = new EmbedBuilder
			{
				Color = new Color(47, 111, 146),
				Description = $"Here are some commands like **{command}**"
			};

			foreach (CommandMatch match in result.Commands)
			{
				CommandInfo cmd = match.Command;

				// Builds the help string.
				builder.AddField(
					string.Join(", ", cmd.Aliases),
					$"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
					$"Summary: {cmd.Summary}\n" +
					$"Instructions: {cmd.Remarks}\n" +
					$"Aliases: {string.Join(", ", cmd.Aliases)}");
			}

			// Replies normally if a direct message fails.
			try
			{
				await Context.User.SendMessageAsync(string.Empty, false, builder.Build());
			}
			catch
			{
				await ReplyAsync(string.Empty, false, builder.Build());
			}
		}

		[Command("about")]
		[Summary("`>about` Displays information about the bot.")]
		public async Task AboutAsync()
		{
			DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;

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

			embed.AddInlineField("Written by", "[TopHATTwaffle](https://github.com/tophattwaffle)");
			embed.AddInlineField(
				"With Help From",
				"[BenBlodgi](https://github.com/BenVlodgi)\n" +
				"[Mark](https://github.com/MarkKoz)\n" +
				"[JimWood](https://github.com/JamesT-W)");
			embed.AddInlineField(
				"Build Date",
				$"{buildDate}\n[Changelog](https://github.com/tophattwaffle/BotHATTwaffle/commits/master)");
			embed.AddInlineField(
				"Built With",
				"[Discord.net V1.0.2](https://github.com/RogueException/Discord.Net)\n" +
				"[CoreRCON](https://github.com/ScottKaye/CoreRCON)\n" +
				"[Html Agility Pack](http://html-agility-pack.net/)\n" +
				"[Newtonsoft Json.NET](https://www.newtonsoft.com/json)\n" +
				"[SSH.NET](https://github.com/sshnet/SSH.NET/)\n" +
				"[FluentFTP](https://github.com/robinrodricks/FluentFTP)");

			await ReplyAsync(string.Empty, false, embed.Build());
		}
	}
}
