using System.ComponentModel.DataAnnotations;

namespace BotHATTwaffle.Models
{
	public class Shitpost
	{
		//This is a table in the Master.sqlite DB
		[Key]
		public int seq_id { get; set; }

		public string snowflake { get; set; }
		public string username { get; set; }
		public string shitpost { get; set; }
		public string fullmessage { get; set; }
		public string date { get; set; }

	}
}
/*
CREATE TABLE `shitposts` (
	`seq_id`	INTEGER PRIMARY KEY AUTOINCREMENT,
	`snowflake`	TEXT,
	`username`	TEXT,
	`shitpost`	TEXT,
	`full_text`	TEXT,
	`date`	TEXT
);*/
