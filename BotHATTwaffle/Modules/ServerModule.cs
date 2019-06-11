using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using BotHATTwaffle.Models;
using BotHATTwaffle.Objects;
using BotHATTwaffle.Objects.Json;
using BotHATTwaffle.Services;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BotHATTwaffle.Modules
{
	public class ServerModule : ModuleBase<SocketCommandContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly DataServices _data;
		private readonly ServerService _server;

		public ServerModule(DiscordSocketClient client, DataServices data, ServerService server)
		{
			_client = client;
			_data = data;
			_server = server;
		}

		[Command("reserve")]
		[Summary("Reserves a public server under the invoking user for personal testing purposes.")]
		[Remarks("A reservation lasts 2 hours. A Workshop ID can be included in order to have that map automatically hosted.")]
		[Alias("PublicServer", "ps")]
		[RequireContext(ContextType.Guild)]
		[RequireRole(Role.ActiveMember)]
		public async Task PublicTestStartAsync(
			[Summary("The three-letter code which identifies the server to reserve.")]
			string serverCode,
			[Summary("The ID of a Steam Workshop map for the server to host.")]
			string mapId = null)
		{
			if (!_levelTesting.CanReserve)
			{
				await ReplyAsync($"```Servers cannot be reserved at this time." +
					$"\nServer reservation is blocked 1 hour before a scheduled test, and resumes once the calendar event has passed.```");
				return;
			}

			ServerService.Result result =
				await _server.ReserveAsync(Context.Message.Author, DateTime.Now.AddHours(2), serverCode);

			switch (result.Status)
			{
				case ServerService.Status.Success:
					await HandleSuccess();

					break;
				case ServerService.Status.ServerNotFound:
					await HandleNotFound();

					break;
				case ServerService.Status.ServerReserved:
					await HandleReserved();

					break;
				case ServerService.Status.UserHasReservation:
					await ReplyAsync(
						$"```You already have a reservation for {result.Reservation.Server.Name}. " +
						$"You have {result.Reservation.GetTimeLeft():h\'H \'m\'M\'} left.```");

					break;
			}

			#region Handlers

			async Task HandleSuccess()
			{
				LevelTestingServer server = result.Reservation.Server;
				string avatar = Context.Message.Author.GetAvatarUrl();

				var embed = new EmbedBuilder
				{
					ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
					Color = new Color(243, 128, 72),
					Description =
						$"For the next two hours, you can use `{Program.COMMAND_PREFIX}pc [command]` to send commands to " +
						$"the server. To see a list of available server commands, use `{Program.COMMAND_PREFIX}pc`. " +
						"Use `>help pc` for more information.\n" +
						"Your control of the server will be revoked after two hours.\n\n" +
						"*If you cannot connect to the reserved server for any reason, please inform TopHATTwaffle!*"
				};

				embed.WithAuthor($"Hey there, {Context.Message.Author}! You have {server.Address} for 2 hours.", avatar);
				embed.WithFooter("This is in beta; please inform TopHATTwaffle of any issues.", avatar);
				embed.AddField("Connect Info", $"`connect {server.Address}`");
				embed.AddField(
					"Links",
					"[Schedule a Playtest](https://www.tophattwaffle.com/playtesting/) | " +
					"[View Testing Calendar](http://playtesting.tophattwaffle.com)");

				await ReplyAsync(string.Empty, false, embed.Build());

				// Changes the map if one is provided.
				if (mapId != null)
				{
					await Task.Delay(3000);
					await _data.RconCommand("host_workshop_map " + mapId, server);
				}
			}

			async Task HandleReserved()
			{
				Reservation reservation = result.Reservation;

				var builder = new EmbedBuilder
				{
					ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
					Color = new Color(243, 128, 72),
					Description =
						$"You cannot reserve the server {reservation.Server.Name} because someone else is using it. " +
						$"Their reservation ends in {reservation.GetTimeLeft():h\'H \'m\'M\'}\n" +
						$"Use `{Program.COMMAND_PREFIX}sr` to see all current server reservations."
				};

				builder.WithAuthor(
					$"Unable to reserve a server for {Context.Message.Author.Username}!",
					Context.Message.Author.GetAvatarUrl());

				await ReplyAsync(string.Empty, false, builder.Build());
			}

			async Task HandleNotFound()
			{
				var embed = new EmbedBuilder
				{
					ThumbnailUrl = "https://www.tophattwaffle.com/wp-content/uploads/2017/11/1024_png-300x300.png",
					Color = new Color(243, 128, 72),
					Description =
						$"A server with code '{serverCode}' could not be found." +
						$"\nUse `{Program.COMMAND_PREFIX}servers` to see all servers."
				};

				embed.WithAuthor($"Hey there, {Context.Message.Author.Username}!", Context.Message.Author.GetAvatarUrl());

				await ReplyAsync(string.Empty, false, embed.Build());
			}

			#endregion
		}

		[Command("ReleaseServer")]
		[Summary("Releases the invoking user's reservation for a public server.")]
		[Alias("rs")]
		[RequireContext(ContextType.Guild)]
		[RequireRole(Role.ActiveMember)]
		public async Task ReleaseServerAsync()
		{
			if (!_levelTesting.CanReserve)
			{
				await ReplyAsync($"```Servers cannot be reserved at this time." +
								 $"\nServer reservation is blocked 1 hour before a scheduled test, and resumes once the calendar event has passed.```");
				return;
			}

			ServerService.Result result = await _server.ClearReservationAsync(Context.Message.Author.Id);

			switch (result.Status)
			{
				case ServerService.Status.Success:
					await ReplyAsync($"```Releasing your server reservation for {result.Reservation.Server.Name}.```");

					break;
				case ServerService.Status.ReservationNotFound:
					await ReplyAsync(
						"```No server reservation for you could be found. " +
						$"See `{Program.COMMAND_PREFIX}help ps` for information on reserving a server.```");

					break;
			}
		}

		// TODO: Cleanup.
		[Command("command")]
		[Summary("Invokes a command on the invoking user's reserved test server.")]
		[Remarks("One must have a server already reserved to use this command.")]
		[Alias("PublicCommand", "pc")]
		[RequireContext(ContextType.Guild)]
		[RequireRole(Role.ActiveMember)]
		public async Task PublicTestCommandAsync([Remainder]string command = null)
		{
			EmbedBuilder embed = _server.GetEmbedBase();

			if (!_levelTesting.CanReserve)
			{
				embed.Title = "\U0001F6AB Server Reservations Closed"; // prohibited
				embed.Description =
					"Servers cannot be reserved at this time.\nReservations are closed one hour before a scheduled " +
					"playtesting event; they are re-opened once the event ends.";

				await ReplyAsync(string.Empty, false, embed.Build());

				return;
			}

			// Displays all whitelisted server commands.
			if (command == null)
			{
				embed.Title = "\U0001F4C3 Whitelisted Server Commands"; // page with curl
				embed.Description = string.Join(", ", _data.PublicCommandWhiteList.Select(c => $"`{c}`"));

				await ReplyAsync(string.Empty, false, embed.Build());

				return;
			}

			Reservation reservation = _server.GetReservation(Context.Message.Author.Id);

			if (reservation == null)
			{
				embed.Title = "\u274C No Reservation Found"; // cross mark
				embed.Description = $"No server reservation for `{Context.Message.Author.Mention}` could be found." +
									$"See `{Program.COMMAND_PREFIX}help ps` for information on reserving a server.";

				await ReplyAsync(string.Empty, false, embed.Build()); // TODO: Remove mention if it notifies users.

				return;
			}

			if (command.Contains(";"))
			{
				embed.Title = "\u26A0 Invalid Character"; // warning
				embed.Description = "Server commands cannot contain semicolons `;`.";

				await ReplyAsync(string.Empty, false, embed.Build());

				return;
			}

			// TODO: Is the below method acceptable, or does the supplied whitelist rely on partial matches?
			// if (_data.PublicCommandWhiteList.Contains(command.Split(' ')[0], StringComparer.OrdinalIgnoreCase))
			if (_data.PublicCommandWhiteList.Any(s => command.Contains(s, StringComparison.OrdinalIgnoreCase)))
			{
				string reply = await _data.RconCommand(command, reservation.Server);
				Console.WriteLine("RCON:\n" + reply);

				// Removes log messages from the reply.
				reply = Regex.Replace(reply, @"^L .*$", string.Empty, RegexOptions.Multiline);
				reply = reply.Replace("discord.gg", "discord,gg").Replace(reservation.Server.Password, "[PASSWORD HIDDEN]");

				embed.Title = "\u2705 Server Command Sent"; // white heavy check mark

				// Special case for handling passwords.
				if (command.Contains("sv_password"))
				{
					await Context.Message.DeleteAsync(); //Message was setting password, delete it.
					embed.Description = $"A command which sets the server password was sent to `{reservation.Server.Name}`.\n";
				}
				else
				{
					embed.Description = $"The command `{command}` was sent to `{reservation.Server.Name}`.\n```" + reply;

					if (embed.Description.Length > 2042)
					{
						embed.Description = embed.Description.Truncate(2042) + "...```";
						embed.WithFooter("The command's reply was truncated due to Discord's limits.");
					}
				}

				await ReplyAsync(string.Empty, false, embed.Build());
				await _data.ChannelLog(
					$"{Context.User} sent an RCON command using public command",
					$"{command} was sent to: {reservation.Server.Address}\n{reply}");

				return;
			}

			embed.Title = "\u274C Invalid Command"; // cross mark
			embed.Description =
				$"The command `{command}` cannot be sent to `{reservation.Server.Name}` because that command is disallowed.\n" +
				$"Use `{Program.COMMAND_PREFIX}pc` to see all commands that can be sent to the server.";

			await ReplyAsync(string.Empty, false, embed);
		}

		[Command("extend")]
		[Summary("Extends the invoking user's reservation by 30 minutes.")]
		public async Task ExtendAsync()
		{
			Reservation reservation = _server.GetReservation(Context.Message.Author.Id);
			EmbedBuilder embed = _server.GetEmbedBase();

			if (reservation.Extend())
			{
				embed.Title = "\u2705 Reservation Extended"; // white heavy check mark
				embed.Description = "Your server reservation has been extended by 30 minutes.";
			}
			else
			{
				embed.WithTitle("\u274C Extension Failure"); // cross mark
				embed.Description = "Reservations with more than 30 minutes remaining cannot be extended.";
			}

			embed.WithFooter("Time of expiration");
			embed.Timestamp = reservation.Expiration;
			embed.Description += $"Time remaining: {reservation.GetTimeLeft():h\'H \'m\'M\'}";

			await ReplyAsync(string.Empty, false, embed.Build());
		}

		[Command("servers")]
		[Summary("Lists all servers.")]
		[Alias("list", "ListServers", "ls")]
		[RequireContext(ContextType.Guild)]
		public async Task ListServersAsync()
		{
			EmbedBuilder embed = _server.GetEmbedBase();
			embed.Title = "\U0001f4c3 Servers"; // page with curl

			foreach (LevelTestingServer server in _server.GetServers())
				embed.AddField(server.Address, $"Prefix: `{server.Name}`\n{server.Description}");

			await ReplyAsync(string.Empty, false, embed.Build());
		}

		[Command("reservations")]
		[Summary("Lists all currently reserved servers.")]
		[Alias("ListReservations", "lr", "ShowReservations", "sr")]
		[RequireContext(ContextType.Guild)]
		public async Task ListReservationsAsync()
		{
			EmbedBuilder embed = _server.GetEmbedBase();

			foreach (Reservation r in _server.GetReservations())
			{
				embed.AddField(
					r.Server.Address,
					$"Reserver: {r.Reserver.Mention}\nTime remaining: {r.GetTimeLeft():h\'H \'m\'M\'}");
			}

			if (embed.Fields.Any())
				embed.Title = "\U0001f4c3 Current Server Reservations"; // page with curl
			else
			{
				embed.Title = "\u26A0 No Reservations Found"; // warning
				embed.Description =
					"No server reservations were found. " +
					$"See `{Program.COMMAND_PREFIX}help reserve` for information on how to reserve your own server!";
			}

			await ReplyAsync(string.Empty, false, embed.Build());
		}

		[Command("ClearReservations")]
		[Summary("Clears server reservations.")]
		[Remarks("If no server is specified, _all_ reservations are cleared.")]
		[Alias("cr")]
		[RequireContext(ContextType.Guild)]
		[RequireRole(Role.Moderators)]
		public async Task ClearReservationsAsync(
			[Summary("The three-letter code which identifies the server to clear.")]
			string serverCode = null)
		{
			if (serverCode == null)
				await _server.ClearReservationsAsync();
			else
				await _server.ClearReservationAsync(serverCode);

			await ListReservationsAsync();
		}
	}
}
