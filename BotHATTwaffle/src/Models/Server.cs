using System.ComponentModel.DataAnnotations;

namespace BotHATTwaffle.Models
{
	public class Server
	{
		//This is a table in the Master.sqlite DB
		[Key]
		public string name { get; set; }

		public string description { get; set; }
		public string address { get; set; }
		public string rcon_password { get; set; }
		public string ftp_path { get; set; }
		public string ftp_username { get; set; }
		public string ftp_password { get; set; }
		public string ftp_type { get; set; }

		public override string ToString() => $"{nameof(name)}: {name}, {nameof(description)}: {description}, {nameof(address)}: {address}, {nameof(rcon_password)}: {rcon_password}, {nameof(ftp_path)}: {ftp_path}, {nameof(ftp_username)}: {ftp_username}, {nameof(ftp_password)}: {ftp_password}, {nameof(ftp_type)}: {ftp_type}";
	}
}