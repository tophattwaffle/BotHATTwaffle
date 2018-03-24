using BotHATTwaffle.Models;
using BotHATTwaffle.Services;

using Microsoft.EntityFrameworkCore;

namespace BotHATTwaffle
{
	public class DataBaseContext : DbContext
	{
		private string path;
		public DataBaseContext()
		{
			path = DataService.dbPath;
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionbuilder)
		{
			optionbuilder.UseSqlite($"Data Source={path}");
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