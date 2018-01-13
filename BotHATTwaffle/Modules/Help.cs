using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
//using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Discord.WebSocket;

namespace BotHATTwaffle.Modules
{
	public class HelpModule : ModuleBase<SocketCommandContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _service;
		//private readonly IConfigurationRoot _config;

		public HelpModule(DiscordSocketClient client, CommandService service)
		{
			_client = client;
			_service = service;
			//_config = config;
		}

		/// <summary>
		/// Sends a DM with general help message if possible. If not, replies with it.
		/// </summary>
		/// <returns></returns>
		[Command("help")]
		[Summary("`>help` Displays this message")]
		[Alias("h")]
		public async Task HelpAsync()
		{
			//If in a DM, don't try to delete their message
			if (!Context.IsPrivate)
				await Context.Message.DeleteAsync();

			//string prefix = _config["prefix"];
			var builder = new EmbedBuilder()
			{
				Color = new Color(47, 111, 146),
				Description = "These are the commands you can use"
			};

			//Loop through all of our modules
			foreach (var module in _service.Modules)
			{
				//Build the help string
				string description = null;
				foreach (var cmd in module.Commands)
				{
					var result = await cmd.CheckPreconditionsAsync(Context);
					if (result.IsSuccess)
						description += $"{cmd.Name} - {cmd.Summary}\n";
				}

				if (!string.IsNullOrWhiteSpace(description))
				{
					builder.AddField(x =>
					{
						x.Name = module.Name;
						x.Value = description;
						x.IsInline = false;
					});
				}
			}

			//Try to DM, if we can't reply
			try
			{
				await Context.User.SendMessageAsync("", false, builder.Build());
			}
			catch
			{
				await ReplyAsync("", false, builder.Build());
			}
		}

		/// <summary>
		/// Gives a help message for a specific command with more detail.
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		[Command("help")]
		[Summary("`>help [command]` Displays help message for a specific command")]
		[Alias("h")]
		public async Task HelpAsync(string command)
		{
			if (!Context.IsPrivate)
				await Context.Message.DeleteAsync();
			var result = _service.Search(Context, command);
			if (!result.IsSuccess)
			{
				await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
				return;
			}

			//string prefix = _config["prefix"];
			var builder = new EmbedBuilder()
			{
				Color = new Color(47, 111, 146),
				Description = $"Here are some commands like **{command}**"
			};

			//Loop through all of our commands that match the search
			foreach (var match in result.Commands)
			{
				//Build the help string
				var cmd = match.Command;
				builder.AddField(x =>
				{
					x.Name = string.Join(", ", cmd.Aliases);
					x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}" +
							  $"\nSummary: {cmd.Summary}" + $"\nInstructions: {cmd.Remarks}" +
							  $"\nAlias: {string.Join(", ", cmd.Aliases.ToArray())}";
					;
					x.IsInline = false;
				});
			}

			//Try to DM, if we can't reply
			try
			{
				await Context.User.SendMessageAsync("", false, builder.Build());
			}
			catch
			{
				await ReplyAsync("", false, builder.Build());
			}
		}

		/// <summary>
		/// Displays the about message.
		/// </summary>
		/// <returns></returns>
		[Command("about")]
		[Summary("`>about` Displays information about the bot")]
		public async Task AboutAsync()
		{
			DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;

			List<EmbedFieldBuilder> fieldBuilder = new List<EmbedFieldBuilder>();
			fieldBuilder.Add(new EmbedFieldBuilder
			{
				Name = "Written by",
				Value = "[TopHATTwaffle](https://github.com/tophattwaffle)",
				IsInline = true
			});
			fieldBuilder.Add(new EmbedFieldBuilder
			{
				Name = "With Help From",
				Value = "[BenBlodgi](https://github.com/BenVlodgi)\n[Mark](https://github.com/MarkKoz)\n[JimWood](https://github.com/JamesT-W)",
				IsInline = true
			});
			fieldBuilder.Add(new EmbedFieldBuilder
			{
				Name = "Build Date",
				Value = $"{buildDate}\n[Changelog](https://github.com/tophattwaffle/BotHATTwaffle/commits/master)",
				IsInline = true
			});
			fieldBuilder.Add(new EmbedFieldBuilder
			{
				Name = "Built With",
				Value = $"[Discord.net V1.0.2](https://github.com/RogueException/Discord.Net)" +
						$"\n[CoreRCON](https://github.com/ScottKaye/CoreRCON)" +
						$"\n[Html Agility Pack](http://html-agility-pack.net/)" +
						$"\n[Newtonsoft Json.NET](https://www.newtonsoft.com/json)" +
						$"\n[SSH.NET](https://github.com/sshnet/SSH.NET/)" +
						$"\n[FluentFTP](https://github.com/robinrodricks/FluentFTP)",
				IsInline = true
			});


			//string prefix = _config["prefix"];
			var authBuilder = new EmbedAuthorBuilder()
			{
				Name = "About BotHATTwaffle",
				IconUrl = "https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png",
			};

			var builder = new EmbedBuilder()
			{
				Fields = fieldBuilder,
				Author = authBuilder,
				Url = "https://www.tophattwaffle.com/",
				ThumbnailUrl = _client.CurrentUser.GetAvatarUrl(),
				Color = new Color(130, 171, 206),
				Description =
					"BotHATTwaffle was started to centralize Source Engine Discord server functions that were fractured between multiple bots. " +
					"This bot was my first attempt at a real C# program that other people would interact with." +
					"\n\nPlease let me know if you have any suggests or find bugs!"
			};
			await ReplyAsync("", false, builder.Build());
		}
	}
}
