using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Open.HttpProxy.BufferManager;

namespace Open.HttpProxy
{
	internal class ServerHandler
	{
		private readonly Session _session;
		private Connection _connection;
		private StreamWriter _streamWriter;

		public ServerHandler(Session session)
		{
			_session = session;
		}

		public async Task ConnectToHostAsync()
		{
			var uri = GetUriFromRequest();
			var dnsEndPoint = new DnsEndPoint(uri.DnsSafeHost, uri.Port);
			var ipAddresses = await Task<IPAddress[]>.Factory.FromAsync(
				Dns.BeginGetHostAddresses,
				Dns.EndGetHostAddresses,
				uri.DnsSafeHost, null);

			foreach (var ipAddress in ipAddresses)
			{
				try
				{
					_connection = new Connection(new IPEndPoint(ipAddress, uri.Port));
					await _connection.ConnectAsync();
					var stream = new ManualBufferedStream(new ConnectionStream(_connection), _session.BufferAllocator);
					_streamWriter = new StreamWriter(stream);
					//_streamWriter.NewLine = "\f";
					break;
				}
				catch
				{
					_connection.Close();	
				}
			}
		}

		private Uri GetUriFromRequest()
		{
			var requestUri = _session.Request.RequestLine.Uri;
			var requestHost = _session.Request.Headers.Host;
			if (requestUri == "*")
			{
				return new Uri(requestHost, UriKind.Relative);
			}
			if (Uri.IsWellFormedUriString(requestUri, UriKind.Absolute))
			{
				return new Uri(requestUri, UriKind.Absolute);
			}
			if (Uri.IsWellFormedUriString(requestUri, UriKind.Relative))
			{
				return new Uri(new Uri(requestHost), requestUri);
			}
			throw new Exception();
		}

		public async Task SendEntityAsync()
		{
			await _streamWriter.WriteLineAsync(_session.Request.RequestLine.ToString());
			await _streamWriter.WriteLineAsync();
			await _streamWriter.WriteLineAsync(_session.Request.Headers.ToCharArray());
			//await _streamWriter.WriteLineAsync("Powered-By: Open.HttpProxy");
			await _streamWriter.WriteLineAsync();
		}

		public async Task SendBodyAsync()
		{
			await _streamWriter.WriteAsync(_session.Request.Body);
			await _streamWriter.FlushAsync();
		}

		public Task ReceiveEntityAsync()
		{
			throw new NotImplementedException();
		}

		public Task ReceiveBodyAsync()
		{
			throw new NotImplementedException();
		}
	}
}