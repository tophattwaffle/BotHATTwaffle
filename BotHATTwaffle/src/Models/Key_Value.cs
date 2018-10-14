using System.ComponentModel.DataAnnotations;

namespace BotHATTwaffle.Models
{
    public class Key_Value
    {
        //This is a table in the Master.sqlite DB
        [Key]
        public string key { get; set; }

        public string value { get; set; }
    }
}
