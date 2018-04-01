using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Discord.WebSocket;

namespace BotHATTwaffle.Services
{
    /// <inheritdoc />
    public class TimerService : ITimerService
    {
        private readonly DiscordSocketClient _client;
        private readonly DataService _data;
        private readonly Random _random;
        private readonly Timer _timer;
        private readonly ConcurrentBag<Func<Task>> _callbacks = new ConcurrentBag<Func<Task>>();

        /// <summary>
        /// Initialises a timer with a start delay <see cref="DataService.StartDelay"/> and a period of
        /// <see cref="DataService.UpdateInterval"/>, as defined in the config.
        /// </summary>
        public TimerService(DiscordSocketClient client, DataService data, Random rand)
        {
            _client = client;
            _data = data;
            _random = rand;

            AddCallback(ChangePlayingAsync);

            _timer = new Timer(
                async _ =>
                {
                    // Tasks have to be created every period from the functions because they cannot be re-used after completion.
                    IEnumerable<Task> tasks = _callbacks.Select(f => f());

                    // Probably not enough of a performance gain to outweigh the overhead.
                    // ParallelQuery<Task> tasks = Functions.AsParallel().Select(f => f());

                    await Task.WhenAll(tasks);
                },
                null,
                TimeSpan.FromSeconds(_data.StartDelay),
                TimeSpan.FromSeconds(_data.UpdateInterval));
        }

        ~TimerService() => _timer.Dispose(); // TODO: Could block until all callbacks complete before finishing disposal.

        /// <inheritdoc />
        public bool Running { get; private set; }

        /// <inheritdoc />
        public void AddCallback(Func<Task> callback) => _callbacks.Add(callback);

        // TODO: RemoveFunction could be added, but it's not currently needed.
        // It would involve using a ConcurrentDictionary with a dummy value or implementing a custom concurrent HashSet type.

        /// <inheritdoc />
        public void Start()
        {
            if (Running) return;

            _timer.Change(TimeSpan.FromSeconds(_data.StartDelay), TimeSpan.FromSeconds(_data.UpdateInterval));
            Running = true;
            Console.WriteLine($"Timer started with an interval of {_data.UpdateInterval} seconds.\n");
        }

        /// <inheritdoc />
        public void Stop()
        {
            if (!Running) return;

            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            Running = false;
            Console.WriteLine("Timer stopped!\n");
        }

        /// <summary>
        /// Changes the playing/status message of the bot.
        /// </summary>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        // TODO: Move elsewhere.
        private async Task ChangePlayingAsync() =>
            await _client.SetGameAsync(_data.PlayingStrings.ElementAt(_random.Next(0, _data.PlayingStrings.Count)));
    }
}
