using Newtonsoft.Json;


namespace BotHATTwaffle.Modules.Json
{
	class JsonTutorial
	{
		[JsonProperty("url")]
		public string url { get; set; }
		[JsonProperty("tags")]
		public string[] tags { get; set; }//List<string> tags { get; set; }
	}
}
