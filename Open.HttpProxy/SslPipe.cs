using System;
using System.IO;
using System.Net.Security;
using System.Runtime.Remoting.Messaging;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	class SslPipe : Pipe
	{
		private readonly string _host;

		public SslPipe(Stream stream, string host)
			: base(new SslStream(stream, false))
		{
			_host = host;
		}

		public override async Task StartAsync()
		{
			var cert = await CertificateProvider.GetCertificateForHost(_host);
			var sslStream = (SslStream) Stream;
			await sslStream.AuthenticateAsServerAsync(cert, false, SslProtocols.Default, true);
		}
	}
}
