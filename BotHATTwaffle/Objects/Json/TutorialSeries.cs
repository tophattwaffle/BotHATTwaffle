using System.Collections.Generic;

using Newtonsoft.Json;

namespace BotHATTwaffle.Objects.Json
{
	public class TutorialSeries
	{
		[JsonProperty("tutorial")]
		public List<Tutorial> Tutorials { get; set; }
	}
}
