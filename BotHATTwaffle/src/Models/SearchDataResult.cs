using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BotHATTwaffle.Models
{
    public class SearchDataResult
    {
        //This is a table in the Master.sqlite DB
        [Key]
        public string name { get; set; }

        public string url { get; set; }

        public virtual ICollection<SearchDataTag> tags { get; set; }
    }
}
