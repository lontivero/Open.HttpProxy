using System;
using System.Net;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	internal class ServerHandler
	{
		private readonly Session _session;
		private Pipe _pipe;

		public ServerHandler(Session session)
		{
			_session = session;
		}

		public async Task ConnectToHostAsync()
		{
			var uri = GetUriFromRequest();
			var ipAddresses = await Task<IPAddress[]>.Factory.FromAsync(
				Dns.BeginGetHostAddresses,
				Dns.EndGetHostAddresses,
				uri.DnsSafeHost, null);

			foreach (var ipAddress in ipAddresses)
			{
				Connection connection = null;
				try
				{
					connection = new Connection(new IPEndPoint(ipAddress, uri.Port));
					await connection.ConnectAsync();
					var stream = new ConnectionStream(connection);
					_pipe = new Pipe(stream);
					break;
				}
				catch
				{
					connection?.Close();	
				}
			}
		}

		private Uri GetUriFromRequest()
		{
			var requestUri = _session.Request.RequestLine.Authority;
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
			throw new Exception($"Error: invalid {requestUri}");
		}

		public async Task SendEntityAsync()
		{
			await _pipe.Writer.WriteRequestLineAsync(_session.Request.RequestLine);
			await _pipe.Writer.WriteHeadersAsync(_session.Request.Headers);
		}

		public async Task SendBodyAsync()
		{
			await _pipe.Writer.WriteBodyAsync(_session.Request.Body);
		}

		public async Task ReceiveEntityAsync()
		{
			var parser = new ResponseParser(_pipe.Reader);
			_session.Response = await parser.ParseAsync();
		}

		public async Task ReceiveBodyAsync()
		{
			if (_session.Response.HasBody)
			{
				_session.Response.Body = _session.Response.IsChunked
					? await _pipe.Reader.ReadChunckedBodyAsync()
					: await _pipe.Reader.ReadBodyAsync(_session.Response.Headers.ContentLength.Value);
				if (_session.Response.IsChunked)
				{
					_session.Response.Headers.Add("Content-Length", _session.Response.Body.Length.ToString());
					_session.Response.Headers.Remove("Transfer-Encoding");
				}
			}
		}
	}
}