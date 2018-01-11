using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using BotHATTwaffle;
using BotHATTwaffle.Modules;
using BotHATTwaffle.Objects.Downloader;
using Discord.Addons.Interactive;

public class Program
{
	private CommandService _commands;
	public static DiscordSocketClient Client;
	public IServiceProvider Services;
	private Eavesdropping _eavesdrop;

	//Logging information.
	private const string _LOG_PATH = "c:/BotHATTwafflelogs/";
	private readonly string _logFile = $"{DateTime.Now:hh_mmtt-MM_dd_yyyy}.log";

	private static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

	public async Task StartAsync()
	{
		Console.Title = "BotHATTwaffle";

		//Mirror output to a log file in case I need it later.
		//This will automatically mirror all text sent to the console to a text file.
		Directory.CreateDirectory(_LOG_PATH);
		var cc = new ConsoleCopy(_LOG_PATH + _logFile);

		//DI all of our services
		Client = new DiscordSocketClient();
		_commands = new CommandService();

		Services = new ServiceCollection()
			.AddSingleton(Client)
			.AddSingleton(_commands)
			.AddSingleton<TimerService>()
			.AddSingleton<UtilityService>()
			.AddSingleton<ModerationServices>()
			.AddSingleton<LevelTesting>()
			.AddSingleton<ToolsService>()
			.AddSingleton<Eavesdropping>()
			.AddSingleton<DataServices>()
			.AddSingleton<Random>()
			.AddSingleton<DownloaderService>()
			.AddSingleton(s => new InteractiveService(Client, TimeSpan.FromSeconds(120)))
			.BuildServiceProvider();

		//Start what services need to be started.
		//This will call the code inside the service's constructor.
		Services.GetRequiredService<DataServices>();
		Services.GetRequiredService<TimerService>();

		//Grab the bot's token from the config files.
		//This is the only setting that has to be retreived this way so it can start up properly.
		//Once the guild becomes ready the rest of the settings are fully loaded.
		string botToken = null;
		if (Services.GetRequiredService<DataServices>().Config.ContainsKey("botToken"))
			botToken = (Services.GetRequiredService<DataServices>().Config["botToken"]);

		//Setup our eavesdropping object
		_eavesdrop = new Eavesdropping(Services.GetRequiredService<DataServices>());

		//Event Subscriptions
		Client.Log += Log;

		//When a user joins the server
		Client.UserJoined += _eavesdrop.UserJoin;

		//When a guild is available
		Client.GuildAvailable += Client_GuildAvailable;

		await InstallCommandsAsync();

		await Client.LoginAsync(TokenType.Bot, botToken);
		await Client.StartAsync();

		//Subscribe to connect/disconnect after actually connecting to prevent them from triggering before we need them.
		Client.Disconnected += Client_Disconnected;
		Client.Connected += Client_Connected;

		await Task.Delay(-1);
	}

	/// <summary>
	/// Fired when the client connects to Discord.
	/// </summary>
	/// <returns></returns>
	private Task Client_Connected()
	{
		Console.WriteLine($"\n{DateTime.Now}\nCLIENT CONNECTED\n");

		Services.GetRequiredService<TimerService>().Restart();

		return Task.CompletedTask;
	}

	/// <summary>
	/// Fired when the client disconnects from Discord.
	/// </summary>
	/// <param name="arg">Disconnect Reason</param>
	/// <returns></returns>
	private Task Client_Disconnected(Exception arg)
	{
		Console.WriteLine(
			$"\n{DateTime.Now}\nCLIENT DISCONNECTED\nMessage: {arg.Message}\n---STACK TRACE---\n{arg.StackTrace}\n\n");

		Services.GetRequiredService<TimerService>().Stop();

		return Task.CompletedTask;
	}

	/// <summary>
	/// When the guilde becomes avaiable. Meaning we can start looking are roles/users
	/// that belong in this guild.
	/// </summary>
	/// <param name="arg"></param>
	/// <returns></returns>
	private Task Client_GuildAvailable(SocketGuild arg)
	{
		Services.GetRequiredService<DataServices>().ReloadSettings();

		return Task.CompletedTask;
	}

	private Task Log(LogMessage arg)
	{
		Console.WriteLine(arg);

		return Task.CompletedTask;
	}

	public async Task InstallCommandsAsync()
	{
		// Hook the MessageReceived Event into our Command Handler
		Client.MessageReceived += HandleCommandAsync;

		// Discover all of the commands in this assembly and load them.
		await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
	}

	/// <summary>
	/// Used on every command that the bot sees. The entry point for running commands.
	/// This is run every time the MessageRecieved even is raised.
	/// </summary>
	/// <param name="messageParam">The message that was read</param>
	/// <returns></returns>
	private async Task HandleCommandAsync(SocketMessage messageParam)
	{
		//Bot's prefix character
		const char prefixChar = '>';

		// Don't process the command if it was a System Message
		var message = (SocketUserMessage)messageParam;

		if (message == null) return;

		// Create a number to track where the prefix ends and the command begins
		int argPos = 0;

		// Determine if the message is a command, based on if it starts with prefixChar or a mention prefix
		if (!(message.HasCharPrefix(prefixChar, ref argPos) || message.HasMentionPrefix(Client.CurrentUser, ref argPos)))
		{
			//The message isn't a command. Lets eavesdrop on it to see if we should do something else. We should not wait for  Low priority.
			Task fireAndForget = _eavesdrop.Listen(messageParam);
			return;
		}

		// Create a Command Context
		var context = new SocketCommandContext(Client, message);

		// Execute the command. (result does not indicate a return value,
		// rather an object stating if the command executed successfully)
		var result = await _commands.ExecuteAsync(context, argPos, Services);

		//Message sending failed.
		if (!result.IsSuccess)
		{
			Console.ForegroundColor = ConsoleColor.Red;

			//If true, this will alert TopHATTwaffle of the error in the log channel.
			bool alert = false;

			if (result.ErrorReason == "Unknown command.")
			{
				//Do nothing. No reason to spam because we use the same prefix as GreenTexts.
			}
			else if (result.ErrorReason == "The input text has too many parameters.")
			{
				//Too many params, cut off the excess for the help reply.
				await context.Channel.SendMessageAsync(
					$"You provided too many parameters! Please consult `>help {context.Message.Content.Substring(1, context.Message.Content.IndexOf(" ") - 1)}`");
			}
			else if (result.ErrorReason == "The input text has too few parameters.")
			{
				//Too few, just reply back with help
				await context.Channel.SendMessageAsync(
					$"You provided too few parameters! Please consult `>help {context.Message.Content.Substring(1)}`");
			}
			else
			{
				//A real error occurred. We can go ahead and log
				alert = true;
				await context.Channel.SendMessageAsync("Something bad happened! I logged the error for TopHATTwaffle.");
			}

			//Let's log the message with diagnostic information. Alert TopHATTwaffle.
			await Services.GetRequiredService<DataServices>().ChannelLog($"An error occurred!\nInvoking command: {context.Message}",
					$"Invoking User: {message.Author}\nChannel: {message.Channel}\nError Reason: {result.ErrorReason}",alert);

			Console.ResetColor();
		}
	}
}
