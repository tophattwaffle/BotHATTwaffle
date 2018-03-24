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

        public override string ToString() => $"{name}|{description}|{address}|RCON_PASSWORD_HIDDEN|{ftp_path}|{ftp_username}|FTP_PASSWORD_HIDDEN|{ftp_type}";
    }
}