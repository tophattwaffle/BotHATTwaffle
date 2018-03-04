using System.Collections.Generic;

using Newtonsoft.Json;

namespace BotHATTwaffle.Models
{
	public class TutorialSeries
	{
		[JsonProperty("tutorial")]
		public List<Tutorial> Tutorials { get; set; }
	}
}
