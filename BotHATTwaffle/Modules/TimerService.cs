#pragma warning disable CS4014 //Disable the async warning. #yolo
using System;
using System.Threading;
using Discord.Commands;
using Discord.WebSocket;

namespace BotHATTwaffle.Modules
{
    public class TimerService
    {
        
        private readonly Timer _timer;
        private LevelTesting _levelTesting;
        private UtilityService _utility;
        private ModerationServices _mod;
        private DiscordSocketClient _client;
        private Random _random;
        DataServices _dataServices;

        public TimerService(DiscordSocketClient client, ModerationServices mod, UtilityService utility, LevelTesting levelTesting, Random rand, DataServices dataServices)
        {
            _dataServices = dataServices;
            _client = client;
            _mod = mod;
            _utility = utility;
            _levelTesting = levelTesting;
            _random = rand;

            //Code inside this will fire ever {updateInterval} seconds
            _timer = new Timer(_ =>
            {
                _levelTesting.Announce();
                _levelTesting.CheckServerReservations();
                _mod.Cycle();
                ChangePlaying();
            },
            null,
            TimeSpan.FromSeconds(_dataServices.startDelay),  // Time that message should fire after bot has started
            TimeSpan.FromSeconds(_dataServices.updateInterval)); // Time after which message should repeat (`Timeout.Infinite` for no repeat)
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Restart()
        {
            _timer.Change(TimeSpan.FromSeconds(_dataServices.startDelay), TimeSpan.FromSeconds(_dataServices.updateInterval));
        }

        public void ChangePlaying()
        {
            _client.SetGameAsync(_dataServices.playingStrings[_random.Next(0, _dataServices.playingStrings.Length)]);
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
}