using System.Collections.Generic;

using Newtonsoft.Json;

namespace BotHATTwaffle.Objects.Json
{
	public class JsonRoot
	{
		[JsonProperty("series")]
		public List<TutorialSeries> Series { get; set; }
		[JsonProperty("servers")]
		public List<LevelTestingServer> Servers { get; set; }
	}
}
