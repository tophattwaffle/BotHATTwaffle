using System;

using Newtonsoft.Json;

namespace BotHATTwaffle.Objects.Json
{
	public class LevelTestingServer : IEquatable<LevelTestingServer>
	{
		[JsonProperty("name")]
		public string Name { get; private set; }
		[JsonProperty("description")]
		public string Description { get; private set; }
		[JsonProperty("address")]
		public string Address { get; private set; }
		[JsonProperty("password")]
		public string Password { get; private set; }
		[JsonProperty("ftppath")]
		public string FtpPath { get; private set; }
		[JsonProperty("ftpuser")]
		public string FtpUser { get; private set; }
		[JsonProperty("ftppass")]
		public string FtpPass { get; private set; }
		[JsonProperty("ftptype")]
		public string FtpType { get; private set; }

		public bool Equals(LevelTestingServer other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;

			return Equals((LevelTestingServer)obj);
		}

		public override int GetHashCode() => Name != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Name) : 0;
	}
}
