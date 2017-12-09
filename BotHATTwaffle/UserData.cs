using Discord.WebSocket;
using System;

namespace BotHATTwaffle
{
    public class UserData
    {
        public SocketGuildUser user { get; set; }
        public DateTime unmuteTime { get; set; }

        public UserData()
        {
        }

        public Boolean CanUnmute()
        {
            if (unmuteTime.CompareTo(DateTime.Now) < 0)
                return true;
            return false;
        }
    }
}
