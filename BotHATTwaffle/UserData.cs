using Discord;
using Discord.WebSocket;
using System;

namespace BotHATTwaffle
{
    public class UserData
    {
        public SocketGuildUser user { get; set; }
        public DateTime unmuteTime { get; set; }
        public DateTime joinRoleTime { get; set; }
        public Embed joinMessage { get; set; }

        public UserData()
        {
        }

        public bool CanUnmute()
        {
            return unmuteTime.CompareTo(DateTime.Now) < 0;
        }

        public bool CanRole()
        {
            return joinRoleTime.CompareTo(DateTime.Now) < 0;
        }
    }
}
