﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotHATTwaffle.Models
{
	public class CommandUse
	{
		//This is a table in the Master.sqlite DB
		[Key]
		public int seq_id { get; set; }

		public long snowflake { get; set; }
		public string username { get; set; }
		public string command { get; set; }
		public string fullmessage { get; set; }
		public string date { get; set; }
	}
}