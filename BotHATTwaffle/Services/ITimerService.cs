using System.Timers;

namespace BotHATTwaffle.Services
{
	/// <summary>
	/// Service for running functions on a timer.
	/// </summary>
	public interface ITimerService
	{
		/// <summary>
		/// Subscribes to the <see cref="Timer.Elapsed"/> event with the given <paramref name="handler"/>.
		/// </summary>
		/// <param name="handler">The event handler with which to subscribe.</param>
		void AddHandler(ElapsedEventHandler handler);

		/// <summary>
		/// Starts the timer.
		/// </summary>
		/// <remarks>
		/// The timer doesn't start raising <see cref="Timer.Elapsed"/> until one interval elapses after startup.
		/// </remarks>
		void Start();

		/// <summary>
		/// Stops the timer.
		/// </summary>
		void Stop();
	}
}
