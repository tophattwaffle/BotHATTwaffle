using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotHATTwaffle.Models
{
    [Table("mutes")]
    public class Mute
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        internal long _userId { get; set; }

        [Required]
        [Column("user_name")]
        public string Username { get; set; }

        [Column("reason")]
        public string Reason { get; set; }

        [Column("duration")]
        public long? Duration { get; set; }

        [Required]
        [Column("muter_name")]
        public string MuterName { get; set; }

        [Column("timestamp")]
        public long UnixTimeSeconds { get; set; }

        [Column("expired")]
        public bool Expired { get; set; } = false;

        [NotMapped]
        public ulong UserId
        {
            get => (ulong)_userId;
            set => _userId = unchecked((long)value);
        }

        [NotMapped]
        public DateTimeOffset Timestamp
        {
            get => DateTimeOffset.FromUnixTimeSeconds(UnixTimeSeconds);
            set => UnixTimeSeconds = value.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Determines if the mute has expired.
        /// </summary>
        /// <returns><c>true</c> if expired; <c>false</c> if still active.</returns>
        public bool CheckExpired() => (DateTimeOffset.UtcNow - Timestamp).TotalMinutes > Duration;
    }
}