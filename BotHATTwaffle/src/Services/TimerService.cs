using System;
using System.Timers;

using Discord.WebSocket;

namespace BotHATTwaffle.Services
{
	/// <inheritdoc />
	public class TimerService : ITimerService
	{
		private readonly DiscordSocketClient _client;
		private readonly DataServices _data;
		private readonly Random _random;
		private readonly Timer _timer;

		/// <summary>
		/// Initialises a timer with an interval of UpdateInterval as defined in the config.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="data"></param>
		/// <param name="rand"></param>
		public TimerService(DiscordSocketClient client, DataServices data, Random rand)
		{
			_client = client;
			_data = data;
			_random = rand;

			_timer = new Timer(TimeSpan.FromSeconds(_data.UpdateInterval).TotalMilliseconds);
			_timer.Elapsed += ChangePlayingAsync;
		}

		~TimerService() => _timer.Dispose();

		/// <inheritdoc />
		public void AddHandler(ElapsedEventHandler handler) => _timer.Elapsed += handler;

		/// <inheritdoc />
		public void Start()
		{
			if (_timer.Enabled) return;

			_timer.Start();
			Console.WriteLine($"Timer started with an interval of {_data.UpdateInterval} seconds.\n");
		}

		/// <inheritdoc />
		public void Stop()
		{
			if (!_timer.Enabled) return;

			_timer.Stop();
			Console.WriteLine("Timer stopped!\n");
		}

		/// <summary>
		/// Changes the playing/status message of the bot.
		/// </summary>
		/// <returns>No object or value is returned by this method when it completes.</returns>
		// TODO: Move elsewhere.
		private async void ChangePlayingAsync(object sender, ElapsedEventArgs e) =>
			await _client.SetGameAsync(_data.PlayingStrings[_random.Next(0, _data.PlayingStrings.Length)]);
	}
}
