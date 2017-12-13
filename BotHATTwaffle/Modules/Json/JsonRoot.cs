using Newtonsoft.Json;
using System.Collections.Generic;

namespace BotHATTwaffle.Modules.Json
{
    class JsonRoot
    {
        [JsonProperty("series")]
        public List<JsonSeries> series { get; set; }
        [JsonProperty("servers")]
        public List<JsonServer> servers { get; set; }
    }
}