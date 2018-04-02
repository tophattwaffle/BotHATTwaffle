using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BotHATTwaffle.Services;
using BotHATTwaffle.Services.Download;
using BotHATTwaffle.Services.Playtesting;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

namespace BotHATTwaffle
{
    public class Program
    {
        public const char COMMAND_PREFIX = '>';

        private CommandService _commands;
        private DiscordSocketClient _client;
        private IServiceProvider _services;
        private DataService _data;
        private ITimerService _timer;
        private MessageListener _messageListener;

        /// <summary>
        /// The entry point of the program. Creates an asyncronous environment to run the bot.
        /// </summary>
        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        /// <summary>
        /// Logs a message to the standard output stream.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private static Task LogEventHandler(LogMessage message)
        {
            Console.WriteLine(message);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initialises the bot and its required services and then subscribes to its events.
        /// </summary>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        public async Task MainAsync()
        {
            Console.Title = "BotHATTwaffle";

            // Concurrently writes the standard output stream to a log file.
            const string LOG_PATH = "c:/BotHATTwafflelogs/";
            string logName = $"{DateTime.Now:hh_mmtt-MM_dd_yyyy}.log";
            Directory.CreateDirectory(LOG_PATH);
            var _ = new ConsoleCopy(LOG_PATH + logName);

            // Dependency injection. All objects use constructor injection.
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<ITimerService, TimerService>()
                .AddSingleton<PlaytestingService>()
                .AddSingleton<MessageListener>()
                .AddSingleton<DataService>()
                .AddSingleton<Random>()
                .AddSingleton<DownloadService>()
                .AddSingleton<EventCalendarService>()
                .AddSingleton<IHelpService, HelpService>()
                .AddSingleton<IMuteService, MuteService>()
                .AddSingleton(s => new InteractiveService(_client, TimeSpan.FromSeconds(120)))
                .BuildServiceProvider();

            // Retrieves services that this class uses.
            _data = _services.GetRequiredService<DataService>();
            _timer = _services.GetRequiredService<ITimerService>();
            _messageListener = _services.GetRequiredService<MessageListener>();

            // Constructs services explicitly. Modules are transient so their dependencies would normally be constructed when
            // the module is initially used e.g. a command is invoked.
            _services.GetRequiredService<IHelpService>();
            _services.GetRequiredService<PlaytestingService>();

            // Retrieves the bot's token from the config file; effectively exits the program if botToken can't be retrieved.
            // This is the only setting that has to be retrieved this way so it can start up properly.
            // Once the guild becomes ready the rest of the settings are fully loaded.
            if (!_data.Config.TryGetValue("botToken", out string botToken)) return;

            // Event subscriptions.
            _client.GuildAvailable += GuildAvailableEventHandler;
            _client.Log += LogEventHandler;
            _client.Ready += ReadyEventHandler;
            _client.UserJoined += UserJoinedEventHandler;

            await InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, botToken);
            await _client.StartAsync();

            // Subscribes to connect/disconnect after logging in because they would otherwise be raised before needed.
            _client.Disconnected += DisconnectedEventHandler;
            _client.Connected += ConnectedEventHandler;

            await Task.Delay(Timeout.Infinite); // Blocks this task until the program is closed.
        }

        /// <summary>
        /// Subscribes to <see cref="DiscordSocketClient.MessageReceived"/> to enable listening for commands and loads all
        /// <see cref="ModuleBase"/>s (which contain commands) in this assembly.
        /// </summary>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private async Task InstallCommandsAsync()
        {
            // TODO: Event not yet implemented in Discord.Net 1.0.
            // _commands.CommandExecuted += CommandExecutedEventHandler;
            _client.MessageReceived += MessageReceivedEventHandler;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        /// <summary>
        /// Processes a command.
        /// </summary>
        /// <remarks>
        /// Creates a <see cref="SocketCommandContext"/> from the <see paramref="message"/>, executes the command, and finally
        /// simulates raising <see cref="CommandService.CommandExecuted"/> by calling <see cref="CommandExecutedEventHandler"/>
        /// to handle the result of the command's execution.
        /// </remarks>
        /// <param name="message">The message which contains the command.</param>
        /// <param name="argPos">The index of the <see paramref="message"/>'s contents at which the command begins.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private async Task ProcessCommandAsync(SocketUserMessage message, int argPos)
        {
            var context = new SocketCommandContext(_client, message);

            // Executes the command; this is not the return value of the command.
            // Rather, it is an object that contains information about the outcome of the execution.
            IResult result = await _commands.ExecuteAsync(context, argPos, _services);

            await CommandExecutedEventHandler(context, result);
        }

        /// <summary>
        /// Raised when the client connects to Discord.
        /// <para>
        /// Restarts the <see cref="TimerService"/> and logs that the client has connected.
        /// </para>
        /// </summary>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private Task ConnectedEventHandler()
        {
            Console.WriteLine($"\n{DateTime.Now}\nCLIENT CONNECTED\n");
            _timer.Start(); // TODO: Remove if it is determined that it is redundant due to ReadyHandler.

            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when the client disconnects from Discord.
        /// <para>
        /// Stops the <see cref="TimerService"/> and logs that the client has disconnected along with exception information.
        /// </para>
        /// </summary>
        /// <param name="e">The exception thrown on disconnect.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private Task DisconnectedEventHandler(Exception e)
        {
            Console.WriteLine(
                $"\n{DateTime.Now}\nCLIENT DISCONNECTED\nMessage: {e.Message}\n---STACK TRACE---\n{e.StackTrace}\n\n");
            _timer.Stop();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when the guild (server) becomes avaiable.
        /// <para>
        /// Calls for the configuration to be read from the file.
        /// </para>
        /// </summary>
        /// <remarks>
        /// The configuration is called to be read here because some configuration fields are parsed into objects. Some of this
        /// parsing requires the guild to be available so that names and roles can be retrieved.
        /// Because this bot is intended to be used on only one server, this should only get raised once.
        /// </remarks>
        /// <param name="guild">The guild that has become available.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private async Task GuildAvailableEventHandler(SocketGuild guild)
        {
            await _data.DeserialiseConfig();
            _services.GetServices<IMuteService>();
        }

        /// <summary>
        /// Raised when a message is received.
        /// <para>
        /// Listens to all messages with <see cref="MessageListener"/> and determines if messages are commands.
        /// </para>
        /// </summary>
        /// <param name="messageParam">The message recieved.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private async Task MessageReceivedEventHandler(SocketMessage messageParam)
        {
            // Ignores system messages.
            if (!(messageParam is SocketUserMessage message))
                return;

            var argPos = 0; // Integer used to track where the prefix ends and the command begins.

            // Determines if the message is a command based on if it starts with the prefix character or a mention prefix.
            if (message.HasCharPrefix(COMMAND_PREFIX, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                await ProcessCommandAsync(message, argPos);

            Task _ = _messageListener.Listen(messageParam); // Fired and forgotten.
        }

        /// <summary>
        /// Raised when guild data has finished downloading.
        /// </summary>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private Task ReadyEventHandler()
        {
            _timer.Stop();
            _timer.Start();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Raised when a user joins a guild.
        /// </summary>
        /// <param name="user">The user which joined.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        internal async Task UserJoinedEventHandler(SocketGuildUser user)
        {
            await _messageListener.WelcomeMessageDm(user);
            await _data.GeneralChannel.SendMessageAsync(
                $"Welcome {user.Mention} to the Source Engine Discord!\n" +
                "Over the next 10 minutes while we verify your account, please check out <#195009920414908416> for the rules.");
        }

        /// <summary>
        /// Raised when a command is executed.
        /// <para>
        /// Handles failed executions of commands. The failure is logged and a message may be sent indicating failure. If an
        /// exception was thrown, the stack trace is printed to the standard output stream.
        /// </para>
        /// </summary>
        /// <remarks>
        /// It is intended to eventully subscribe to the <see cref="CommandService.CommandExecuted"/> event with this handler.
        /// However, it is not yet implemented on Discord.Net 1.0 Therefore, it is raised manually in
        /// <see cref="ProcessCommandAsync"/>. Meanwhile, the <see cref="CommandInfo"/> parameter is excluded from the signature;
        /// no practical way of obtaining it currently exists.
        /// </remarks>
        /// <param name="context">The context in which the command was executed.</param>
        /// <param name="result">The result of the command's execution.</param>
        /// <returns>No object or value is returned by this method when it completes.</returns>
        private async Task CommandExecutedEventHandler(ICommandContext context, IResult result)
        {
            if (result.Error is null || result.Error == CommandError.UnknownCommand)
                return; // Ignores successful executions and unknown commands.

            Console.ForegroundColor = ConsoleColor.Red;
            var alert = false; // Set to true if the log message should mention the appropriate users to alert them of the error.
            string logMessage =
                $"Invoking User: {context.Message.Author}\nChannel: {context.Message.Channel}\nError Reason: {result.ErrorReason}";

            switch (result.Error)
            {
                case CommandError.BadArgCount:
                    string determiner = result.ErrorReason == "The input text has too many parameters." ? "many" : "few";

                    // Retrieves the command's name from the message by finding the first word after the prefix. The string will
                    // be empty if somehow no match is found.
                    string commandName =
                        Regex.Match(context.Message.Content, COMMAND_PREFIX + @"(\w+)", RegexOptions.IgnoreCase).Groups[1].Value;

                    await context.Channel.SendMessageAsync(
                        $"You provided too {determiner} parameters! Please consult `{COMMAND_PREFIX}help {commandName}`");

                    break;
                case CommandError.ParseFailed:
                case CommandError.UnmetPrecondition:
                case CommandError.ObjectNotFound:
                    await context.Channel.SendMessageAsync(result.ErrorReason);

                    break;
                case CommandError.Exception:
                    alert = true;
                    await context.Channel.SendMessageAsync("Something bad happened! I logged the error for TopHATTwaffle.");

                    Exception e = ((ExecuteResult)result).Exception;

                    logMessage += $"\nException: {e.GetType()}\nMethod: {e.TargetSite.Name}";
                    Console.WriteLine($"{e.GetType()}\n{e.StackTrace}\n");

                    break;
                default:
                    alert = true;
                    await context.Channel.SendMessageAsync("Something bad happened! I logged the error for TopHATTwaffle.");

                    break;
            }

            await _data.ChannelLog(
                $"An error occurred!\nInvoking command: {context.Message}",
                logMessage,
                alert);
            Console.ResetColor();
        }
    }
}
