using System;

namespace Open.HttpProxy
{
	public class RequestLine
	{
		public string Verb { get; private set; }
		public string Authority { get; private set; }
		public string Version { get; private set; }
		public DnsEndPoint EndPoint { get; private set; }

		public Uri Uri { get; private set; }

		public RequestLine(string line)
		{
			var ifs = line.IndexOf(' ');
			var ils = line.LastIndexOf(' ');
			Verb = line.Substring(0, ifs);
			Authority = line.Substring(ifs + 1, ils - ifs - 1);
			Version = line.Substring(ils + 1);

			Uri = new Uri(Authority);
			EndPoint = new DnsEndPoint(Uri.Host, Uri.Port);
			return;

			int port = 443;
			string host;

			var c = Authority.IndexOf(':');
			if (c < 0)
			{
				host = Authority;
			}
			else if (c == Authority.Length - 1)
			{
				host = Authority.TrimEnd('/');
			}
			else
			{
				host = Authority.Substring(0, c);
				port = int.Parse(Authority.Substring(c + 1));
			}
			EndPoint = new DnsEndPoint(host, port);
		}

		public override string ToString()
		{
			return $"{Verb} {Authority} {Version}";
		}

		public string ToString2()
		{
			return $"{Verb} {Uri.PathAndQuery} {Version}";
		}

	}
}