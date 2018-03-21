using System.ComponentModel.DataAnnotations;

namespace BotHATTwaffle.Models
{
	public class CommandUse
	{
		//This is a table in the Master.sqlite DB
		[Key]
		public int seq_id { get; set; }

		public string snowflake { get; set; }
		public string username { get; set; }
		public string command { get; set; }
		public string fullmessage { get; set; }
		public string date { get; set; }
	}
}
/*
 * CREATE TABLE `Command_usage` (
	`seq__id`	INTEGER PRIMARY KEY AUTOINCREMENT,
	`snowflake`	TEXT,
	`username`	TEXT,
	`command`	TEXT,
	`fullmessage`	TEXT,
	`date`	TEXT
);
*/
