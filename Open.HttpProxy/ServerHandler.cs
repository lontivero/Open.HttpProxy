using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Open.HttpProxy.BufferManager;

namespace Open.HttpProxy
{
	internal class ServerHandler
	{
		private readonly Session _session;
		private readonly Connection _serverConnection;
		private readonly Pipe _pipe;

		public ServerHandler(Session session, Connection serverConnection)
		{
			_session = session;
			_serverConnection = serverConnection;
//			_pipe = new Pipe(new BufferedStream(new ConnectionStream(serverConnection)));
			_pipe = new Pipe(new BufferedStream(new ConnectionStream(serverConnection), 16 * 1024));
		}

		public static async Task<Connection> ConnectToHostAsync(Uri uri)
		{
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
					return connection;
				}
				catch(SocketException)
				{
					connection?.Close();
				}
			}
			return null;
		}

		public async Task SendEntityAsync()
		{
			await _pipe.Writer.WriteRequestLineAsync(_session.Request.RequestLine);
			await _pipe.Writer.WriteHeadersAsync(TransformHeaders(_session.Request.Headers));
		}

		private IEnumerable<KeyValuePair<string, string>> TransformHeaders(HttpRequestHeaders headers)
		{
			if (headers.ProxyConnection == null) return headers;
			var filteredHeaders = headers
				.Where(x => !x.Key.Equals("proxy-connection", StringComparison.OrdinalIgnoreCase))
				.Where(x => !x.Key.Equals("connection", StringComparison.OrdinalIgnoreCase))
				.ToDictionary(entry => entry.Key, entry => entry.Value);
			filteredHeaders.Add("Connection", headers.ProxyConnection);
			return filteredHeaders;
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