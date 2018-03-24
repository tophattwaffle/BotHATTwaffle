using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotHATTwaffle.Models
{
    public class ActiveMute
    {
        //This is a table in the Master.sqlite DB
        [Key]
        public long snowflake { get; set; }

        public string username { get; set; }
        public string mute_reason { get; set; }
        public int mute_duration { get; set; }
        public string muted_by { get; set; }
        public long muted_time { get; set; }

        [NotMapped]
        public DateTimeOffset inMuteTimeOffset
        {
            get => DateTimeOffset.FromUnixTimeSeconds(muted_time);
            set => muted_time = value.ToUnixTimeSeconds();
        }

        public override string ToString() => $"{nameof(snowflake)}: {snowflake}, {nameof(username)}: {username}, {nameof(mute_reason)}: {mute_reason}, {nameof(mute_duration)}: {mute_duration}, {nameof(muted_by)}: {muted_by}, {nameof(muted_time)}: {muted_time}, {nameof(inMuteTimeOffset)}: {inMuteTimeOffset}";
    }
}