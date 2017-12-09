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
using System.Threading;

public class Program
{
    private CommandService _commands;
    public static DiscordSocketClient _client;
    private IServiceProvider _services;
    public static Dictionary<string, string> config;
    string logChannelStr;
    string playTesterRoleStr;
    string announcementChannelStr;
    string testingChannelStr;
    public static SocketTextChannel logChannel = null;
    public static SocketTextChannel announcementChannel = null;
    public static SocketTextChannel testingChannel = null;
    public static SocketRole playTesterRole = null;
    public static string serverIconURL = null;

    private static void Main(string[] args) => new Program().StartAsync().GetAwaiter().GetResult();

    public async Task StartAsync()
    {
        config = ReadSettings();

        string botToken = null;
        if (config.ContainsKey("botToken"))
            botToken = (config["botToken"]);
        else
        {
            Console.WriteLine("Cannot find \"botToken\".");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
            return;
        }

        if (config.ContainsKey("announcementChannel"))
            announcementChannelStr = (config["announcementChannel"]);

        if (config.ContainsKey("logChannel"))
            logChannelStr = (config["logChannel"]);

        if (config.ContainsKey("testingChannel"))
            testingChannelStr = (config["testingChannel"]);

        if (config.ContainsKey("playTesterRole"))
            playTesterRoleStr = (config["playTesterRole"]);

        //_utility = new UtilityServices();
        //_mod = new ModerationServices();
        //_testing = new LevelTesting();

        _client = new DiscordSocketClient();
        _commands = new CommandService();
        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .AddSingleton<TimerService>()
            .AddSingleton<UtilityServices>()
            .AddSingleton<ModerationServices>()
            .AddSingleton<LevelTesting>()
            .AddSingleton<Eavesdropping>()
            .AddSingleton<Random>()
            .BuildServiceProvider();

        _services.GetRequiredService<TimerService>();

        Eavesdropping _eavesdrop = new Eavesdropping(config);

        //Event Subscriptions
        _client.Log += Log;
        _client.UserJoined += _eavesdrop.UserJoin;
        _client.MessageReceived += _eavesdrop.Listen;
        _client.GuildAvailable += Client_GuildAvailable;

        await InstallCommandsAsync();

        await _client.LoginAsync(TokenType.Bot, botToken);
        await _client.StartAsync();
        await Task.Delay(-1);
    }

    private Task Client_GuildAvailable(SocketGuild arg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        //Iterate all channels
        foreach (SocketTextChannel s in arg.TextChannels)
        {
            if (s.Name == logChannelStr)
            {
                logChannel = s;
                Console.WriteLine($"\nLog Channel Found! Logging to: {logChannel}");
            }
            if (s.Name == announcementChannelStr)
            {
                announcementChannel = s;
                Console.WriteLine($"\nAnnouncement Channel Found! Announcing to: {announcementChannel}");
            }
            if (s.Name == testingChannelStr)
            {
                testingChannel = s;
                Console.WriteLine($"\nTesting Channel Found! Sending playtest alerts to: {announcementChannel}");
                
            }
        }

        foreach (SocketRole r in arg.Roles)
        {
            if (r.Name == playTesterRoleStr)
            {
                playTesterRole = r;
                Console.WriteLine($"\nPlaytester role found!: {playTesterRole}\n");
            }
        }
        Console.ResetColor();

        return Task.CompletedTask;
    }

    private Task Log(LogMessage arg)
    {
        Console.WriteLine(arg);

        return Task.CompletedTask;
    }

    public static Task ChannelLog(string message)
    {
        logChannel.SendMessageAsync($"\n```{message}```");
        Console.WriteLine($"{DateTime.Now} {message}");

        return Task.CompletedTask;
    }

    public static Task ChannelLog(string title, string message)
    {
        logChannel.SendMessageAsync($"\n```{title}\n\n{message}```");
        Console.WriteLine($"{DateTime.Now} {title}\n{message}");

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
        if (config.ContainsKey("prefixChar"))
            prefixChar = config["prefixChar"][0];
        else
            Console.WriteLine($"Key \"clockDelay\" not found or valid. Using default{prefixChar}.");

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
            await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
    
    //TODO: Move to its own service class that I can DI where needed.
    //Rewrite to be easier to add new keys, and get new keys
    private Dictionary<string, string> ReadSettings()
    {
        string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
        Dictionary<string, string> mainConfig;
        string configPath = "settings.ini";
        if (File.Exists(configPath))
        {
            mainConfig = File.ReadAllLines(configPath).ToDictionary(line => line.Split('=')[0].Trim(), line => line.Split('=')[1].Trim());
        }
        else
        {
            // Config doesn't exist, so we'll make it
            mainConfig = new Dictionary<string, string>();

            //Get INT
            //if (config.ContainsKey("clockDelay"))
            //int.TryParse(config["clockDelay"], out clockDelay);

            //Get String
            //if (config.ContainsKey("botToken"))
            //botToken = (config["botToken"]);


            //Get Char
            //if (config.ContainsKey("prefixChar"))
            //prefixChar = config["prefixChar"][0];
        }

        // Add existing settings at their default
        //General or global
        if (!mainConfig.ContainsKey("botToken"))
            mainConfig.Add("botToken", $"NEEDS_TO_BE_REPLACED");
        if (!mainConfig.ContainsKey("startDelay"))
            mainConfig.Add("startDelay", $"10");
        if (!mainConfig.ContainsKey("updateInterval"))
            mainConfig.Add("updateInterval", $"60");
        if (!mainConfig.ContainsKey("calUpdateTicks"))
            mainConfig.Add("calUpdateTicks", $"1");
        if (!mainConfig.ContainsKey("prefixChar"))
            mainConfig.Add("prefixChar", ">");
        if (!mainConfig.ContainsKey("logChannel"))
            mainConfig.Add("logChannel", $"bothattwaffle_logs");
        if (!mainConfig.ContainsKey("announcementChannel"))
            mainConfig.Add("announcementChannel", $"announcements");
        if (!mainConfig.ContainsKey("playingStringsCSV"))
            mainConfig.Add("playingStringsCSV", $"Eating Waffles,Not working on Titan,The year is 20XX,Hopefully not crashing,>help,>upcoming");
        if (!mainConfig.ContainsKey("agreeUserCSV"))
            mainConfig.Add("agreeUserCSV", $"TopHATTwaffle,Phoby,thewhaleman,maxgiddens,CSGO John Madden,Wazanator,TanookiSuit3,JSadones,Lykrast,maxgiddens,Zelz Storm");

        //Playtesting vars
        if (!mainConfig.ContainsKey("testCalID"))
            mainConfig.Add("testCalID", $"Replace My Buddy");
        if (!mainConfig.ContainsKey("playTesterRole"))
            mainConfig.Add("playTesterRole", $"Playtester");
        if (!mainConfig.ContainsKey("testingChannel"))
            mainConfig.Add("testingChannel", $"csgo_level_testing");

        //Eavesdropping vars
        if (!mainConfig.ContainsKey("pakRatEavesDropCSV"))
            mainConfig.Add("pakRatEavesDrop", $"use pakrat,download pakrat,get pakrat,use packrat");
        if (!mainConfig.ContainsKey("howToPackEavesDropCSV"))
            mainConfig.Add("howToPackEavesDropCSV", $"how do i pack,how can i pack,how to pack,how to use vide,help me pack");
        if (!mainConfig.ContainsKey("carveEavesDropCSV"))
            mainConfig.Add("carveEavesDropCSV", $"how do i carve,use carve,with carve,carve");
        if (!mainConfig.ContainsKey("propperEavesDropCSV"))
            mainConfig.Add("propperEavesDropCSV", $"use propper,download propper,get propper,configure propper,setup propper");
        if (!mainConfig.ContainsKey("vbEavesDropCSV"))
            mainConfig.Add("vbEavesDropCSV", $"velocity brawl,velocitybrawl,velocity ballsack");
        if (!mainConfig.ContainsKey("yorkCSV"))
            mainConfig.Add("yorkCSV", $"de_york,de york");

        //Command Dependent
        if (!mainConfig.ContainsKey("roleMeWhiteListCSV"))
            mainConfig.Add("roleMeWhiteListCSV", $"Programmer,Level_Designer,3D_Modeler,Texture_Artist,Blender,Maya,3dsmax");
        if (!mainConfig.ContainsKey("moderatorRoleName"))
            mainConfig.Add("moderatorRoleName", $"Moderators");
        if (!mainConfig.ContainsKey("mutedRoleName"))
            mainConfig.Add("mutedRoleName", $"Muted");

        //Shitpost vars
        if (!mainConfig.ContainsKey("catFactPath"))
            mainConfig.Add("catFactPath", $"X:\\Scripts\\catfacts.txt");

        // Save new config file
        File.WriteAllLines(configPath, mainConfig.Select(kvp => $"{kvp.Key} = {kvp.Value}").ToArray());

        return mainConfig;
    }
}