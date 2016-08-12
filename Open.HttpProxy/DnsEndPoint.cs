using System;
using System.Net;

namespace Open.HttpProxy
{
	public class DnsEndPoint : EndPoint
	{
		public string Host { get; }
		public int Port { get; }

		public string Domain
		{
			get
			{
				var parts = Host.Split('.');

				if (parts.Length < 3)
					return Host;

				var c = parts.Length;
				if (parts[c - 1].Length < 3 && parts[c - 2].Length <= 3)
					return string.Join(".", parts, c - 3, 3);
				
				return string.Join(".", parts, c - 2, 2);
			}
		}

		public string WildcardDomain
		{
			get
			{
				var host = Host;
				var domain = Domain;

				var subdomainLen = host.Length - domain.Length;
				if (subdomainLen == 0) return domain;

				var subdomain = host.Substring(0, subdomainLen);
				var io = subdomain.IndexOf(".", 0, subdomainLen -1, StringComparison.Ordinal);
				if (io <= -1) return $"*.{domain}";

				var last = subdomain.Substring(io+1);
				return $"*.{last}{domain}";
			}
		}

		public DnsEndPoint(string host, int port)
		{
			Host = host;
			Port = port;
		}
		
		public override bool Equals(object comparand)
		{
			var dnsEndPoint = comparand as DnsEndPoint;
			return dnsEndPoint != null && (Port == dnsEndPoint.Port) && Host == dnsEndPoint.Host;
		}
		
		public override int GetHashCode()
		{
			return StringComparer.InvariantCultureIgnoreCase.GetHashCode(ToString());
		}
		
		public override string ToString()
		{
			return $@"{Host}:{Port}";
		}
	}
}