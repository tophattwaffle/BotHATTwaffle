using System;
using System.Timers;

using Discord.WebSocket;

namespace BotHATTwaffle.Modules
{
	/// <summary>
	/// Service for running functions on a timer.
	/// </summary>
	public class TimerService
	{
		private readonly DiscordSocketClient _client;
		private readonly DataServices _dataServices;
		private readonly Random _random;
		private readonly Timer _timer;

		/// <summary>
		/// Initialises a timer with an interval of UpdateInterval as defined in the config.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="dataServices"></param>
		/// <param name="rand"></param>
		public TimerService(
			DiscordSocketClient client,
			DataServices dataServices,
			Random rand)
		{
			_client = client;
			_dataServices = dataServices;
			_random = rand;

			_timer = new Timer(TimeSpan.FromSeconds(_dataServices.UpdateInterval).TotalMilliseconds);
			_timer.Elapsed += ChangePlaying;
		}

		~TimerService() => _timer.Dispose();

		/// <summary>
		/// Subscribes to the <see cref="Timer.Elapsed"/> event with the given <paramref name="handler"/>.
		/// </summary>
		/// <param name="handler">The event handler with which to subscribe.</param>
		public void AddHandler(ElapsedEventHandler handler) => _timer.Elapsed += handler;

		/// <summary>
		/// Starts the timer.
		/// </summary>
		/// <remarks>
		/// The timer doesn't start raising <see cref="Timer.Elapsed"/> until one interval elapses after startup.
		/// </remarks>
		public void Start()
		{
			if (_timer.Enabled) return;

			_timer.Start();
			Console.WriteLine($"Timer started with an interval of {_dataServices.UpdateInterval} seconds.\n");
		}

		/// <summary>
		/// Stops the timer.
		/// </summary>
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
		public async void ChangePlaying(object sender, ElapsedEventArgs e) =>
			await _client.SetGameAsync(_dataServices.PlayingStrings[_random.Next(0, _dataServices.PlayingStrings.Length)]);
	}
}
