using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BotHATTwaffle.Modules
{
	public class UtilitiesModule : ModuleBase<SocketCommandContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly DataServices _dataServices;

		public UtilitiesModule(DiscordSocketClient client, DataServices dataServices)
		{
			_client = client;
			_dataServices = dataServices;
		}

		/// <summary>
		/// Displays the estimated round-trip latency, in miliseconds, to the gateway server.
		/// </summary>
		/// <seealso cref="DiscordSocketClient.Latency"/>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		[Command("ping")]
		[Summary("`>ping` Replies with the bot's latency to Discord.")]
		[Remarks("The ping is the estimated round-trip latency, in miliseconds, to the gateway server.")]
		public async Task PingAsync()
		{
			var builder = new EmbedBuilder
			{
				Color = new Color(47, 111, 146),
				Description = $"*Do you like waffles?*\nIt took me **{_client.Latency} ms** to reach the Discord API."
			};

			await ReplyAsync(string.Empty, false, builder.Build());
		}

		/// <summary>
		/// Toggles the invoking user's roles.
		/// </summary>
		/// <remarks>
		/// Yields a list of all toggleable roles when invoked without parameters. The roles which can be used with this command
		/// are specified in the <c>roleMeWhiteListCSV</c> config field.
		/// </remarks>
		/// <param name="rolesStr">A case-insensitive space-delimited list of roles to toggle.</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		[Command("roleme")]
		[Summary("`>roleme [role names]` Toggles the invoking user's roles.")]
		[Remarks(
			"This enables one to toggle some of oneself's roles. The toggleable roles typically display possession of a skill, " +
			"such as 3D Modeling or level design.\n" +
			"The command accepts multiple roles in one invocation: `>roleme blender level_designer programmer`.\n" +
			"Invoking it without any parameters, i.e. `>roleme`, yields a list of all available roles.")]
		public async Task RolemeAsync([Remainder]string rolesStr = "display")
		{
			// Currently, the framework to get users' roles in DMs doesn't exist.
			if (Context.IsPrivate)
			{
				await ReplyAsync("**This command can not be used in a direct message.**");
				return;
			}

			if (rolesStr.Equals("display", StringComparison.InvariantCultureIgnoreCase))
			{
				await ReplyAsync($"Valid roles are:```\n{string.Join("\n", _dataServices.RoleMeWhiteList)}```");
				return;
			}

			string[] rolesIn = rolesStr.Split(' ');

			// The intersection of the role whitelist and the inputted roles.
			IEnumerable<string> roleNames =
				_dataServices.RoleMeWhiteList.Intersect(rolesIn, StringComparer.InvariantCultureIgnoreCase);

			// The intersection of the inputted roles and the valid roles.
			// It'd be safer to inserect with the SocketRole collection below,
			// but this assumes that the roles in the whitelist actually exist.
			IEnumerable<string> rolesInvalid = rolesIn.Except(roleNames, StringComparer.InvariantCultureIgnoreCase);

			roleNames = roleNames.Select(r => r.Replace('_', ' '));

			// Finds all SocketRoles from roleNames.
			IEnumerable<SocketRole> roles =
				Context.Guild.Roles.Where(r => roleNames.Contains(r.Name, StringComparer.InvariantCultureIgnoreCase));

			var user = (SocketGuildUser)Context.User;
			var rolesAdded = new List<SocketRole>();
			var rolesRemoved = new List<SocketRole>();

			// Updates roles.
			foreach (SocketRole role in roles)
			{
				if (user.Roles.Contains(role))
				{
					await ((IGuildUser)user).RemoveRoleAsync(role);
					rolesRemoved.Add(role);
				}
				else
				{
					await ((IGuildUser)user).AddRoleAsync(role);
					rolesAdded.Add(role);
				}
			}

			// Builds the response.
			var logMessage = new StringBuilder();

			var embed = new EmbedBuilder();
			embed.WithTitle("`roleme` Results");
			embed.WithDescription($"Results of toggled roles for {Context.User.Mention}:");

			if (rolesAdded.Any())
			{
				string name = $"Added ({rolesAdded.Count})";

				embed.AddInlineField(name, string.Join("\n", rolesAdded.Select(r =>r.Mention)));
				logMessage.AppendLine($"{name}\n    " + string.Join("\n    ", rolesAdded.Select(r => r.Name)));
			}

			if (rolesRemoved.Any())
			{
				string name = $"Removed ({rolesRemoved.Count})";

				embed.AddInlineField(name, string.Join("\n", rolesRemoved.Select(r => r.Mention)));
				logMessage.AppendLine($"{name}\n    " + string.Join("\n    ", rolesRemoved.Select(r => r.Name)));
			}

			if (rolesInvalid.Any())
			{
				string name = $"Failed ({rolesInvalid.Count()})";

				embed.AddInlineField(name, string.Join("\n", rolesInvalid));
				embed.WithFooter("Roles fail if they don't exist or toggling them is disallowed.");
				logMessage.Append($"{name}\n    " + string.Join("\n    ", rolesInvalid));
			}

			await ReplyAsync(string.Empty, false, embed.Build());
			await _dataServices.ChannelLog($"Toggled Roles for {Context.User}", logMessage.ToString());
		}
	}
}
