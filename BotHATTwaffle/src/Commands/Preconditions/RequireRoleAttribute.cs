using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using BotHATTwaffle.Models;

using Discord;
using Discord.Commands;

namespace BotHATTwaffle.Commands.Preconditions
{
	/// <summary>
	/// This attribute requires that the user invoking the command has any of the specified roles.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	public class RequireRoleAttribute : PreconditionAttribute
	{
		public IReadOnlyCollection<ulong> RoleIds { get; }
		public IReadOnlyCollection<string> RoleNames { get; }

		/// <summary>
		/// Require that the user invoking the command has any of the specified roles.
		/// </summary>
		/// <param name="roleIds">The IDs of the roles the user must have.</param>
		public RequireRoleAttribute(params ulong[] roleIds)
		{
			RoleIds = roleIds?.ToImmutableArray();
		}

		/// <summary>
		/// Require that the user invoking the command has any of the specified roles.
		/// </summary>
		/// <param name="roles">The roles the user must have.</param>
		public RequireRoleAttribute(params Role[] roles)
		{
			RoleIds = roles?.Select(r => (ulong)r).ToImmutableArray();
		}

		/// <summary>
		/// Require that the user invoking the command has any of the specified roles.
		/// </summary>
		/// <param name="roleNames">The names of the roles the user must have.</param>
		public RequireRoleAttribute(params string[] roleNames)
		{
			RoleNames = roleNames?.ToImmutableArray();
		}

		public override Task<PreconditionResult> CheckPermissions(
			ICommandContext context,
			CommandInfo command,
			IServiceProvider services)
		{
			if (!(context.User is IGuildUser guildUser))
			{
				return Task.FromResult(
					PreconditionResult.FromError("This command has a role requirement and can only be used within a guild."));
			}

			var allowedRoleIds = new List<ulong>();

			if (RoleIds != null)
				allowedRoleIds.AddRange(RoleIds);

			if (RoleNames != null)
				allowedRoleIds.AddRange(context.Guild.Roles.Where(r => RoleNames.Contains(r.Name)).Select(r => r.Id));

			return guildUser.RoleIds.Intersect(allowedRoleIds).Any()
				? Task.FromResult(PreconditionResult.FromSuccess())
				: Task.FromResult(
					PreconditionResult.FromError(
						"You do not have a role required to invoke this command. " +
						"Any of the following roles will satisfy this requirement: " +
						string.Join(
							", ",
							context.Guild.Roles
								.Where(r => allowedRoleIds.Contains(r.Id))
								.Select(r => $"`{r.Name}`"))));
		}
	}
}
