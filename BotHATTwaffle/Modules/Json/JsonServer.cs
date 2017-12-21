using Newtonsoft.Json;

namespace BotHATTwaffle.Modules.Json
{
    public class JsonServer
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
        public string FTPPath { get; set; }
        [JsonProperty("ftpuser")]
        public string FTPUser { get; set; }
        [JsonProperty("ftppass")]
        public string FTPPass { get; set; }
        [JsonProperty("ftptype")]
        public string FTPType { get; set; }
    }
}
