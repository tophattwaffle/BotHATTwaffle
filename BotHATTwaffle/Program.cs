using System;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using BotHATTwaffle;
using BotHATTwaffle.Modules;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class Program
{
    private CommandService _commands;
    public static DiscordSocketClient _client;
    public IServiceProvider _services;   

    private static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

    public async Task StartAsync()
    {
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
            .BuildServiceProvider();

        _services.GetRequiredService<TimerService>();
        _services.GetRequiredService<DataServices>();

        string botToken = null;
        if (_services.GetRequiredService<DataServices>().config.ContainsKey("botToken"))
            botToken = (_services.GetRequiredService<DataServices>().config["botToken"]);

        Eavesdropping _eavesdrop = new Eavesdropping(_services.GetRequiredService<DataServices>());

        //Event Subscriptions
        _client.Log += Log;
        _client.UserJoined += _eavesdrop.UserJoin;
        _client.MessageReceived += _eavesdrop.Listen;

        await InstallCommandsAsync();

        await _client.LoginAsync(TokenType.Bot, botToken);
        await _client.StartAsync();

        await Task.Delay(3000); //Wait for the bot to connect before moving forward. Needs to be connected to read channel settings.
        _services.GetRequiredService<DataServices>().ReadData(); //Load the rest of our settings
        await Task.Delay(-1);
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
        if (!(message.HasCharPrefix(prefixChar, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;
        // Create a Command Context
        var context = new SocketCommandContext(_client, message);
        // Execute the command. (result does not indicate a return value, 
        // rather an object stating if the command executed successfully)
        var result = await _commands.ExecuteAsync(context, argPos, _services);
        if (!result.IsSuccess)
        {
            Console.WriteLine(result.ErrorReason);
            if (result.ErrorReason != "Unknown command.")
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}