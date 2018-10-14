using System;
using System.Configuration;
using System.IO;

using BotHATTwaffle.Models;

using Microsoft.EntityFrameworkCore;

namespace BotHATTwaffle
{
    public class DataBaseContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // TODO: "Hack" for EF Core tools trying to read ef.dll.config instead of BotHATTwaffle.dll.config.
            string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Configuration config = ConfigurationManager.OpenExeConfiguration(location);
            optionsBuilder.UseSqlite(config.ConnectionStrings.ConnectionStrings["Master"].ConnectionString);
        }

        public DbSet<CommandUse> CommandUsage { get; set; }
        public DbSet<Mute> Mutes { get; set; }
        public DbSet<SearchDataResult> SearchDataResults { get; set; }
        public DbSet<SearchDataTag> SearchDataTags { get; set; }
        public DbSet<Server> Servers { get; set; }
        public DbSet<Key_Value> KeyVaules { get; set; } // TODO: Fix typo & create migration.
        public DbSet<Shitpost> Shitposts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Mute>(
                e =>
                {
                    e.HasIndex(c => new {c._userId, c.UnixTimeSeconds}).IsUnique();
                    e.HasIndex(c => new {c._userId, c.Expired}).IsUnique().HasFilter(@"expired == 0");
                });

            modelBuilder.Entity<SearchDataTag>()
                .HasKey(t => new { t.name, t.tag, t.series });
        }
    }
}
