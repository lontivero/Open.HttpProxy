using System;
using System.Diagnostics;
using System.Security.Policy;

namespace Open.HttpProxy
{
	public class Request : HttpMessage
	{
		public Request()
		{
		}

		public Request(RequestLine requestLine, HttpHeaders headers, byte[] body = null)
		{
			RequestLine = requestLine;
			Headers = headers;
			if(body!=null) Body = body;
		}

		public RequestLine RequestLine { get; set; }

		public DnsEndPoint EndPoint => new DnsEndPoint(Uri.Host, Uri.Port);

		public bool IsHttps =>  RequestLine != null && RequestLine.IsVerb("CONNECT");

		public Uri Uri
		{
			get
			{
				var uri = RequestLine.Uri;

				string scheme;
				var io = uri.IndexOf("://", StringComparison.Ordinal);
				if (io == -1 || io > "https".Length)
				{
					scheme = IsHttps ? "https" : "http";
				}
				else
				{
					scheme = uri.Substring(0, io);
					uri = uri.Substring(io+3);
				}

				string authority=null;
				if (uri[0] != '/')
				{
					authority = IsHttps ? uri : uri.Substring(0, uri.IndexOf("/", StringComparison.Ordinal));
				}

				int port = IsHttps ? 443 : 80;
				string host = null;
				if (authority != null)
				{
					int c = authority.IndexOf(':');
					if (c < 0)
					{
						host = authority;
					}
					else if (c == authority.Length - 1)
					{
						host = authority.TrimEnd('/');
					}
					else
					{
						host = authority.Substring(0, c);
						port = int.Parse(authority.Substring(c + 1));
					}
				}

				if (host == null)
				{
					host = Headers.Host;

					int cp = host.IndexOf(':');
					if (cp >= 0)
					{
						if (cp == host.Length - 1)
							host = host.TrimEnd('/');
						else
						{
							port = int.Parse(host.Substring(cp + 1));
							host = host.Substring(0, cp);
						}
					}
				}
				return new Uri($"{scheme}://{host}:{port}");
			}
		}

		protected override ProtocolVersion GetVersion()
		{
			return RequestLine.Version;
		}

		protected override bool HasContent()
		{
			return (Headers.ContentLength.HasValue && Headers.ContentLength.Value > 0) || IsChunked;
		}

		protected override bool VerifyWebSocketHandshake()
		{
			return "websocket".Equals(Headers.Upgrade, StringComparison.OrdinalIgnoreCase)
				&& RequestLine != null && RequestLine.IsVerb("GET")
				&& ("ws".Equals(Uri.Scheme, StringComparison.OrdinalIgnoreCase)
					|| "wss".Equals(Uri.Scheme, StringComparison.OrdinalIgnoreCase));
		}
	}
}