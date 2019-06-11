using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

using BotHATTwaffle.Objects;
using BotHATTwaffle.Objects.Json;

using Discord;
using Discord.WebSocket;

namespace BotHATTwaffle.Services
{
	public class PlaytestingService : IPlaytestingService
	{
		private readonly AnnouncementService _announcement;
		private readonly DiscordSocketClient _client;
		private readonly DataServices _data;
		private readonly ServerService _server;

		public PlaytestingService(
			DiscordSocketClient client,
			DataServices data,
			ServerService server,
			AnnouncementService announcement)
		{
			_announcement = announcement;
			_client = client;
			_data = data;
			_server = server;
		}

		/// <summary>
		/// Sets up a server for a playtesting event and hosts the appropriate map.
		/// </summary>
		/// <param name="serverCode">The three-letter code which identifies the server on which to host the map.</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		public async Task HostMapAsync(string serverCode)
		{
			LevelTestingServer server = _server.GetServer(serverCode);

			string result = Regex.Match(CurrentEventInfo[6], @"\d+$").Value;

			await _data.RconCommand("exec postgame", server);
			await Task.Delay(5000);
			await _data.RconCommand($"host_workshop_map {result}", server);

			await _data.ChannelLog("Changing Map on Test Server", $"'host_workshop_map {result}' on {server.Address}");
		}

		/// <summary>
		/// Sets up a server for a playtesting event.
		/// </summary>
		/// <param name="serverCode">The three-letter code which identifies the server to set up.</param>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		public async Task SetupServerAsync(string serverCode)
		{
			LevelTestingServer server = _server.GetServer(serverCode);

			var embed = new EmbedBuilder
			{
				Title = "Workshop Link",
				Url = CurrentEventInfo[6],
				ThumbnailUrl = CurrentEventInfo[4],
				Color = new Color(71, 126, 159),
				Description = $"**{server.Description}**\n\n{CurrentEventInfo[9]}"
			};

			embed.AddField("Connect Info", $"`connect {CurrentEventInfo[10]}`");
			embed.WithAuthor(
				$"Setting up {server.Address} for {CurrentEventInfo[2]}",
				"https://cdn.discordapp.com/icons/111951182947258368/0e82dec99052c22abfbe989ece074cf5.png");

			await _data.TestingChannel.SendMessageAsync(string.Empty, false, embed.Build());
			await _data.ChannelLog("Setting postgame config", "'exec postgame' on" + server.Address);
			await _data.RconCommand("exec postgame", server);
		}
	}
}
