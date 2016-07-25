namespace Open.HttpProxy
{
	public class ProtocolVersion
	{
		public string Protocol { get; private set; }
		public int Major { get; private set; }
		public int Minor { get; private set; }

		public static ProtocolVersion Parse(string version)
		{
			var parts = version.Split('/');
			var numparts = parts[1].Split('.');
			var protocol = parts[0];
			var major = int.Parse(numparts[0]);
			var minor = int.Parse(numparts[1]);
			return new ProtocolVersion(protocol, major, minor);
		}

		private ProtocolVersion(string protocol, int major, int minor)
		{
			Protocol = protocol;
			Major = major;
			Minor = minor;
		}

		public override string ToString()
		{
			return $"{Protocol}/{Major}.{Minor}";
		}
	}
}