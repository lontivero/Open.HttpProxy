using System;

namespace Open.HttpProxy
{
	public class RequestLine
	{
		public string Verb { get; }
		public string Uri { get; }
		public ProtocolVersion Version { get; }


		public static RequestLine Parse(string line)
		{
			var ifs = line.IndexOf(' ');
			var ils = line.IndexOf(' ', ifs+1);
			var verb = line.Substring(0, ifs);
			var uri = line.Substring(ifs + 1, ils - ifs - 1);
			var version = ProtocolVersion.Parse(line.Substring(ils + 1));
			return new RequestLine(verb, uri, version);
		}

		public RequestLine(string verb, string uri, ProtocolVersion version)
		{
			Verb = verb;
			Uri = uri;
			Version = version;
		}

		public bool IsVerb(string verb)
		{
			return Verb.Equals(verb, StringComparison.OrdinalIgnoreCase);
		}

		public override string ToString()
		{
			return $"{Verb} {Uri} {Version}";
		}

		public string ToString2()
		{
			return $"{Verb} {Uri} {Version}";
		}

	}
}