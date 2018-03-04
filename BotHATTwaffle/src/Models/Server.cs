using Newtonsoft.Json;

namespace BotHATTwaffle.Models
{
	public class Server
	{
		[JsonProperty("name")]
		public string Name { get; set; }
		[JsonProperty("description")]
		public string Description { get; set; }
		[JsonProperty("address")]
		public string Address { get; set; }
		[JsonProperty("password")]
		public string Password { get; set; }
		[JsonProperty("ftppath")]
		public string FtpPath { get; set; }
		[JsonProperty("ftpuser")]
		public string FtpUser { get; set; }
		[JsonProperty("ftppass")]
		public string FtpPass { get; set; }
		[JsonProperty("ftptype")]
		public string FtpType { get; set; }
	}
}
