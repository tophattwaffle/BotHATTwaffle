using System.Configuration;

using BotHATTwaffle.Models;

using Microsoft.EntityFrameworkCore;

namespace BotHATTwaffle
{
    public class DataBaseContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(ConfigurationManager.ConnectionStrings["Master"].ConnectionString);
        }

        public DbSet<CommandUse> CommandUsage { get; set; }
        public DbSet<Mute> Mutes { get; set; }
        public DbSet<ActiveMute> ActiveMutes { get; set; }
        public DbSet<SearchDataResult> SearchDataResults { get; set; }
        public DbSet<SearchDataTag> SearchDataTags { get; set; }
        public DbSet<Server> Servers { get; set; }
        public DbSet<Key_Value> KeyVaules { get; set; }
        public DbSet<Shitpost> Shitposts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SearchDataTag>()
                .HasKey(t => new { t.name, t.tag, t.series });
        }
    }
}