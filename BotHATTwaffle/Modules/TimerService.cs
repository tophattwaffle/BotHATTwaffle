#pragma warning disable CS4014 //Disable the async warning. #yolo
using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using BotHATTwaffle;
using BotHATTwaffle.Modules;
using Discord.Rest;

public class TimerService
{
    int startDelay = 10;
    int updateInterval = 60;
    private readonly Timer _timer;
    private LevelTesting _levelTesting;
    private UtilityServices _utility;
    private ModerationServices _mod;
    private DiscordSocketClient _client;
    private Random _random;
    string[] playingStrings;

    public TimerService(DiscordSocketClient client, ModerationServices mod, UtilityServices utility, LevelTesting levelTesting, Random rand)
    {
        _client = client;
        _mod = mod;
        _utility = utility;
        _levelTesting = levelTesting;
        _random = rand;

        if ((Program.config.ContainsKey("startDelay") && !int.TryParse(Program.config["startDelay"], out startDelay)))
        {
            Console.WriteLine($"Key \"startDelay\" not found or valid. Using default {startDelay}.");
        }
        if ((Program.config.ContainsKey("updateInterval") && !int.TryParse(Program.config["updateInterval"], out updateInterval)))
        {
            Console.WriteLine($"Key \"updateInterval\" not found or valid. Using default {updateInterval}.");
        }

        if (Program.config.ContainsKey("playingStringsCSV"))
            playingStrings = (Program.config["playingStringsCSV"]).Split(',');

        //Code inside this will fire ever {updateInterval} seconds
        _timer = new Timer(_ =>
        {
            _levelTesting.Announce();
            _mod.Cycle();
            ChangePlaying();
        },
        null,
        TimeSpan.FromSeconds(startDelay),  // Time that message should fire after bot has started
        TimeSpan.FromSeconds(updateInterval)); // Time after which message should repeat (`Timeout.Infinite` for no repeat)
    }

    public void Stop()
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Restart()
    {
        _timer.Change(TimeSpan.FromSeconds(startDelay), TimeSpan.FromSeconds(updateInterval));
    }

    public void ChangePlaying()
    {
        _client.SetGameAsync(playingStrings[_random.Next(0, playingStrings.Length)]);
    }
}

public class TimerModule : ModuleBase
{
    private readonly TimerService _service;

    public TimerModule(TimerService service)
    {

        _service = service;
    }/*
    [Command("stoptimer")]
    public async Task StopCmd()
    {
        _service.Stop();
        await ReplyAsync("Timer stopped.");
    }

    [Command("starttimer")]
    public async Task RestartCmd()
    {
        _service.Restart();
        await ReplyAsync("Timer (re)started.");
    }*/
}