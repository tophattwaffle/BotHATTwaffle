using System;
using System.Threading;
using System.Threading.Tasks;

namespace BotHATTwaffle.Services
{
    /// <summary>
    /// Service for running functions on a timer.
    /// </summary>
    public interface ITimerService
    {
        /// <summary>
        /// Gets a value indicating whether the Timer is running.
        /// </summary>
        bool Running { get; }

        /// <summary>
        /// Adds a function which the timer will execute in parallel with other added functions.
        /// </summary>
        /// <param name="callback">The function to add.</param>
        void AddCallback(Func<Task> callback);

        /// <summary>
        /// Starts the timer after the configured <see cref="DataService.StartDelay"/>.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the timer.
        /// </summary>
        void Stop();
    }
}
