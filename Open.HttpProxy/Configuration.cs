namespace Open.HttpProxy
{
	class Configuration
	{
		public bool UseProxy { get; } = true;

		public DnsEndPoint ProxyEndPoint { get; set; }

		public bool InterceptSsl { get; set; } = true;

		public string RootCACertificateName { get; set; } = "";

		public int MaxConnections { get; set; } = -1;
	}
}
