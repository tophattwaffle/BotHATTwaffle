using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Discord.WebSocket;
using System.Threading;

namespace BotHATTwaffle.Modules
{
	public class UtilityService
	{
		public UtilityService()
		{
			//Nothing happens here, yet
		}
	}

	public class UtilityModule : ModuleBase<SocketCommandContext>
	{
		private readonly DataServices _dataServices;

		public UtilityModule(DataServices dataServices)
		{
			_dataServices = dataServices;
		}

		/// <summary>
		/// Ping command with response time from API
		/// </summary>
		/// <returns></returns>
		[Command("ping")]
		[Summary("`>ping` Replies with a message")]
		[Remarks("It's a ping command.")]
		public async Task PingAsync()
		{
			var replyTime = Program.Client.Latency;
			var builder = new EmbedBuilder()
			{
				Color = new Color(47, 111, 146),
				Description = $"*Do you like waffles?*" +
				$"\nIt took me **{replyTime}ms** to reach the Discord API."
			};
			await ReplyAsync("", false, builder);
		}

		/// <summary>
		/// Allows users to give/remove a role from themselves to display what skills they have.
		/// </summary>
		/// <param name="inRoleStr">List of roles to toggle</param>
		/// <returns></returns>
		[Command("roleme")]
		[Summary("`>roleme [role names]` Toggles roles on a user")]
		[Remarks("This will let you add roles to yourself. Typically for saying you have a skill like 3D Modeling, or level design." +
			"\nYou can put multiple roles into one command to get multiple at one time. Example: `>roleme blender level design programmer`" +
			"\nYou can type `>roleme` to show all roles available")]
		public async Task RolemeAsync([Remainder]string inRoleStr = "display")
		{
			//Currently, we don't have the framework to get user roles in DM.
			//Just don't allow it in a DM.
			if (Context.IsPrivate)
			{
				await ReplyAsync("**This command can not be used in a DM**");
				return;
			}

			var user = Context.User as SocketGuildUser;

			//Display roles, or modify role state
			if (inRoleStr.ToLower() == "display")
			{
				await ReplyAsync($"Valid roles are:```\n{string.Join("\n", _dataServices.RoleMeWhiteList)}```");
			}
			else
			{
				//Validate that we can apply the role
				bool valid = false;
				string reply = null;
				foreach (string s in _dataServices.RoleMeWhiteList)
				{
					if (!inRoleStr.ToLower().Contains(s.ToLower())) continue;
					valid = true; //We applied at least 1 role.

					var inRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == s);

					if (user.Roles.Contains(inRole))
					{
						//Remove role
						reply += $"{Context.User.Username} lost the **{inRole}** role.\n";
						await ((IGuildUser)user).RemoveRoleAsync(inRole);
					}
					else
					{
						//Add role
						reply += $"{Context.User.Username} now has the role **{inRole}**. Enjoy the flair!\n";
						await ((IGuildUser)user).AddRoleAsync(inRole);
					}
				}

				if(valid)
				{
					//Something actually happened - Reply with the changes.
					await ReplyAsync($"{reply}");
					await _dataServices.ChannelLog($"{Context.User}\n{reply}");
				}
				else
				{
					//Nothing was changed - bad input provided.
					await ReplyAsync($"{Context.User.Mention}\n```You cannot assign yourself the role of **{inRoleStr}** because it does not exist, " +
						$"or it is not allowed.```");
					await _dataServices.ChannelLog($"{Context.User} attempted to roleMe the role of: {inRoleStr} and it failed. Either due to the " +
						$"role not existing, or they tried to use a role that isn't in the white list.");
				}
			}
		}
	}
}
