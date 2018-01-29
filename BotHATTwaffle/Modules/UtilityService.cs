using System.Threading.Tasks;
using System.Linq;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BotHATTwaffle.Modules
{
	public class UtilityService
	{
		public UtilityService()
		{
			// Nothing happens here, yet
		}
	}

	public class UtilityModule : ModuleBase<SocketCommandContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly DataServices _dataServices;

		public UtilityModule(DiscordSocketClient client, DataServices dataServices)
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

			await ReplyAsync("", false, builder.Build());
		}

		/// <summary>
		/// Toggles the invoking user's roles.
		/// </summary>
		/// <remarks>
		/// Yields a list of all toggleable roles when invoked without parameters. The roles which can be used with this command
		/// are specified in the <c>roleMeWhiteListCSV</c> config field.
		/// </remarks>
		/// <param name="inRoleStr">List of roles to toggle</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		[Command("roleme")]
		[Summary("`>roleme [role names]` Toggles the invoking user's roles.")]
		[Remarks(
			"This enables one to toggle some of oneself's roles. The toggleable roles tyically display possession of a skill, " +
			"such as 3D Modeling or level design.\n" +
			"The command accepts multiple roles in one invocation: `>roleme blender level design programmer`.\n" +
			"Invoking it without any parameters, i.e. `>roleme`, yields a list of all available roles.")]
		public async Task RolemeAsync([Remainder]string inRoleStr = "display")
		{
			// Currently, the framework to get users' roles in DMs doesn't exist.
			if (Context.IsPrivate)
			{
				await ReplyAsync("**This command can not be used in a direct message.**");
				return;
			}

			var user = (SocketGuildUser)Context.User;

			// Display roles; otherwise toggle them.
			if (inRoleStr.ToLower() == "display")
				await ReplyAsync($"Valid roles are:```\n{string.Join("\n", _dataServices.RoleMeWhiteList)}```");
			else
			{
				// Validates the role can be applied.
				var valid = false;
				string reply = null;

				foreach (string s in _dataServices.RoleMeWhiteList)
				{
					if (!inRoleStr.ToLower().Contains(s.ToLower())) continue;
					valid = true; // At least one role was applied.

					SocketRole inRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == s);

					if (user.Roles.Contains(inRole))
					{
						// Removes the role.
						reply += $"{Context.User.Username} lost the **{inRole}** role.\n";
						await ((IGuildUser)user).RemoveRoleAsync(inRole);
					}
					else
					{
						// Adds the role.
						reply += $"{Context.User.Username} now has the role **{inRole}**. Enjoy the flair!\n";
						await ((IGuildUser)user).AddRoleAsync(inRole);
					}
				}

				if (valid)
				{
					// Something actually happened - reply with the changes.
					await ReplyAsync($"{reply}");
					await _dataServices.ChannelLog($"{Context.User}\n{reply}");
				}
				else
				{
					// Nothing was changed - a bad input was provided.
					await ReplyAsync(
						$"{Context.User.Mention}\n```Not all of the following roles could be toggled: **{inRoleStr}**; some do " +
						"not exist and/or are disallowed.```");
					await _dataServices.ChannelLog(
						$"{Context.User} failed to roleme at least some of the following roles: {inRoleStr}; some do not " +
						"exist and/or are disallowed.");
				}
			}
		}
	}
}
