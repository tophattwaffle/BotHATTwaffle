using BotHATTwaffle.Modules.Json;
using Discord;
using Discord.WebSocket;
using System;

namespace BotHATTwaffle
{
    public class UserData
    {
        public SocketGuildUser User { get; set; }
        public DateTime UnmuteTime { get; set; }
        public DateTime ServerReleaseTime { get; set; }
        public DateTime JoinRoleTime { get; set; }
        public Embed JoinMessage { get; set; }
        public JsonServer Server { get; set; }

        public UserData()
        {
        }

        public bool CanUnmute()
        {
            return UnmuteTime.CompareTo(DateTime.Now) < 0;
        }

        public bool CanReleaseServer()
        {
            return ServerReleaseTime.CompareTo(DateTime.Now) < 0;
        }

        public bool CanRole()
        {
            return JoinRoleTime.CompareTo(DateTime.Now) < 0;
        }
    }
}
