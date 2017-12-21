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

        Eavesdropping _eavesdrop = new Eavesdropping();

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
                Console.WriteLine($"\nTesting Channel Found! Sending playtest alerts to: {testingChannel}");

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
        logChannel.SendMessageAsync($"```{DateTime.Now}\n{message}```");
        Console.WriteLine($"{DateTime.Now}: {message}");

        return Task.CompletedTask;
    }

    public static Task ChannelLog(string title, string message)
    {
        logChannel.SendMessageAsync($"```{DateTime.Now}\n{title}\n{message}```");
        Console.WriteLine($"{DateTime.Now}: {title}\n{message}");

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
            if (result.ErrorReason != "Unknown command.")
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

        #region Add existing settings at their default
        #region General or global
        mainConfig.AddKeyIfMissing("botToken", "NEEDS_TO_BE_REPLACED");
        mainConfig.AddKeyIfMissing("startDelay", "10");
        mainConfig.AddKeyIfMissing("updateInterval", "60");
        mainConfig.AddKeyIfMissing("calUpdateTicks", "1");
        mainConfig.AddKeyIfMissing("prefixChar", ">");
        mainConfig.AddKeyIfMissing("logChannel", "bothattwaffle_logs");
        mainConfig.AddKeyIfMissing("announcementChannel", "announcements");
        mainConfig.AddKeyIfMissing("playingStringsCSV", "Eating Waffles,Not working on Titan,The year is 20XX,Hopefully not crashing,>help,>upcoming");
        mainConfig.AddKeyIfMissing("agreeUserCSV", "TopHATTwaffle,Phoby,thewhaleman,maxgiddens,CSGO John Madden,Wazanator,TanookiSuit3,JSadones,Lykrast,maxgiddens,Zelz Storm");
        #endregion

        #region Playtesting vars
        mainConfig.AddKeyIfMissing("testCalID", "Replace My Buddy");
        mainConfig.AddKeyIfMissing("playTesterRole", "Playtester");
        mainConfig.AddKeyIfMissing("testingChannel", "csgo_level_testing");
        mainConfig.AddKeyIfMissing("demoPath", $"X:\\Playtesting Demos");
        mainConfig.AddKeyIfMissing("casualConfig", $"thw");
        mainConfig.AddKeyIfMissing("compConfig", $"thw");
        mainConfig.AddKeyIfMissing("postConfig", $"postame");
        #endregion

        #region Eavesdropping vars
        mainConfig.AddKeyIfMissing("pakRatEavesDropCSV", "use pakrat,download pakrat,get pakrat,use packrat");
        mainConfig.AddKeyIfMissing("howToPackEavesDropCSV", "how do i pack,how can i pack,how to pack,how to use vide,help me pack");
        mainConfig.AddKeyIfMissing("carveEavesDropCSV", "carve");
        mainConfig.AddKeyIfMissing("propperEavesDropCSV", "use propper,download propper,get propper,configure propper,setup propper");
        mainConfig.AddKeyIfMissing("vbEavesDropCSV", "velocity brawl,velocitybrawl,velocity ballsack");
        mainConfig.AddKeyIfMissing("yorkCSV", "de_york,de york");
        #endregion

        #region Command Dependent
        mainConfig.AddKeyIfMissing("roleMeWhiteListCSV", "Programmer,Level_Designer,3D_Modeler,Texture_Artist,Blender,Maya,3dsmax");
        mainConfig.AddKeyIfMissing("moderatorRoleName", "Moderators");
        mainConfig.AddKeyIfMissing("mutedRoleName", "Muted");
        #endregion

        #region  Shitpost vars
        mainConfig.AddKeyIfMissing("catFactPath", $"X:\\Scripts\\catfacts.txt");
        #endregion

        #endregion

        // Save new config file
        File.WriteAllLines(configPath, mainConfig.Select(kvp => $"{kvp.Key} = {kvp.Value}").ToArray());

        return mainConfig;
    }
}