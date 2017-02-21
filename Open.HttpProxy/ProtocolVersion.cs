namespace Open.HttpProxy
{
	public class ProtocolVersion
	{
		public static readonly ProtocolVersion Http10 = ProtocolVersion.Parse("http/1.0");
		public static readonly ProtocolVersion Http11 = ProtocolVersion.Parse("http/1.1");

		public string Protocol { get; }
		public int Major { get; }
		public int Minor { get; }

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