using System.ComponentModel.DataAnnotations;

namespace BotHATTwaffle.Models
{
	public class SearchDataTag
	{
		//This is a table in the Master.sqlite DB
		public string name { get; set; }
		public string tag { get; set; }
		public string series { get; set; }
		
		public virtual SearchDataResult VirtualSearchDataResult { get; set; }
	}
}
/*
CREATE TABLE `SearchData_Tags` (
	`name`	TEXT,
	`tag`	TEXT,
	`series`	TEXT,
	PRIMARY KEY(`name`,`tag`,`series`)
);
*/
