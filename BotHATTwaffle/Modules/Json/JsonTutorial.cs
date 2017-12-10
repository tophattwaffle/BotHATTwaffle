using Newtonsoft.Json;

namespace TestHATTwaffle
{
    class JsonTutorial
    {
        [JsonProperty("url")]
        public string url { get; set; }
        [JsonProperty("tags")]
        public string[] tags { get; set; }//List<string> tags { get; set; }
    }
}
