using System;
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
    public static DiscordSocketClient _client;
    public IServiceProvider _services;
    Eavesdropping _eavesdrop;

    private static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

    public async Task StartAsync()
    {
        Console.Title = "BotHATTwaffle";

        _client = new DiscordSocketClient();
        _commands = new CommandService();
        _services = new ServiceCollection()
            .AddSingleton(_client)
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
            .AddSingleton(s => new InteractiveService(_client, TimeSpan.FromSeconds(120)))
            .BuildServiceProvider();

        _services.GetRequiredService<DataServices>();
        _services.GetRequiredService<TimerService>();
        

        string botToken = null;
        if (_services.GetRequiredService<DataServices>().config.ContainsKey("botToken"))
            botToken = (_services.GetRequiredService<DataServices>().config["botToken"]);

        _eavesdrop = new Eavesdropping(_services.GetRequiredService<DataServices>());

        //Event Subscriptions
        _client.Log += Log;
        _client.UserJoined += _eavesdrop.UserJoin;
        _client.GuildAvailable += Client_GuildAvailable;

        await InstallCommandsAsync();

        await _client.LoginAsync(TokenType.Bot, botToken);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private Task Client_GuildAvailable(SocketGuild arg)
    {
        _services.GetRequiredService<DataServices>().ReadData();
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
        _client.MessageReceived += HandleCommandAsync;
        // Discover all of the commands in this assembly and load them.
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        char prefixChar = '>';

        // Don't process the command if it was a System Message
        var message = messageParam as SocketUserMessage;
        if (message == null) return;
        // Create a number to track where the prefix ends and the command begins
        int argPos = 0;
        // Determine if the message is a command, based on if it starts with '>' or a mention prefix
        if (!(message.HasCharPrefix(prefixChar, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
        {
            //The message isn't a command. Lets eavesdrop on it to see if we should do something else. We should not wait for this. Low priority.
            Task fireAndForget = _eavesdrop.Listen(messageParam);
            return;
        }
        // Create a Command Context
        var context = new SocketCommandContext(_client, message);
        // Execute the command. (result does not indicate a return value,
        // rather an object stating if the command executed successfully)
        var result = await _commands.ExecuteAsync(context, argPos, _services);
        if (!result.IsSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Boolean alert = true;

            if (result.ErrorReason == "Unknown command.")
            {
                alert = false; //Don't tag me when someone does an unknown command.
            }
            else if (result.ErrorReason == "The input text has too many parameters.")
            {
                await context.Channel.SendMessageAsync($"You provided too many parameters! Please consult `>help {context.Message.Content.Substring(1, context.Message.Content.IndexOf(" ") - 1)}`");
                alert = false;
            }
            else
            {
                await context.Channel.SendMessageAsync("Something bad happened! I logged the error for TopHATTwaffle.");
            }

            await _services.GetRequiredService<DataServices>().ChannelLog($"An error occurred!\nInvoking command: {context.Message}",
                $"Invoking User: {message.Author}\nChannel: {message.Channel}\nError Reason: {result.ErrorReason}", alert);

            Console.ResetColor();
        }

        await _eavesdrop.Listen(messageParam);
    }
}
