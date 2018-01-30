#pragma warning disable CS4014 // Disables the unawaited warning. #yolo

using System;
using System.Threading;

using Discord.WebSocket;

namespace BotHATTwaffle.Modules
{
	public class TimerService
	{
		private readonly DiscordSocketClient _client;
		private readonly DataServices _dataServices;
		private readonly Random _random;
		private readonly Timer _timer;

		public TimerService(
			DiscordSocketClient client,
			DataServices dataServices,
			LevelTesting levelTesting,
			ModerationServices mod,
			Random rand)
		{
			_client = client;
			_dataServices = dataServices;
			_random = rand;

			// Code inside this will fire every {updateInterval} seconds.
			_timer = new Timer(_ =>
			{
				levelTesting.Announce();
				levelTesting.CheckServerReservations();
				mod.Cycle();
				ChangePlaying();
			},
			null,
			TimeSpan.FromSeconds(_dataServices.StartDelay), // Time that message should fire after bot has started.
			TimeSpan.FromSeconds(_dataServices.UpdateInterval)); // Time after which message should repeat (Timeout.Infinite for no repeat).
		}

		public void Stop()
		{
			_timer.Change(Timeout.Infinite, Timeout.Infinite);
			Console.WriteLine("Timer stopped!\n");
		}

		public void Restart()
		{
			_timer.Change(TimeSpan.FromSeconds(_dataServices.StartDelay), TimeSpan.FromSeconds(_dataServices.UpdateInterval));
			Console.WriteLine(
				$"Timer restarted.\nStart Delay: {_dataServices.StartDelay}\nUpdate Interval: {_dataServices.UpdateInterval}\n");
		}

		public void ChangePlaying() =>
			_client.SetGameAsync(_dataServices.PlayingStrings[_random.Next(0, _dataServices.PlayingStrings.Length)]);
	}
}
