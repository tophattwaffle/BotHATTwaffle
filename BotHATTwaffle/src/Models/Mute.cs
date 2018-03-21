using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotHATTwaffle.Models
{
	public class Mute
	{
		//This is a table in the Master.sqlite DB
		[Key]
		public int seq_id { get; set; }

		public long snowflake { get; set; }
		public string username { get; set; }
		public string mute_reason { get; set; }
		public int mute_duration { get; set; }
		public string muted_by { get; set; }
		public string date { get; set; }
	}
}