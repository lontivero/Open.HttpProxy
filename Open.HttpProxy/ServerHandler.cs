using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	internal class ServerHandler
	{
		private readonly Session _session;
		private Pipe _pipe => _session.ServerPipe;

		public ServerHandler(Session session)
		{
			_session = session;
		}

		public static async Task<Connection> ConnectToHostAsync(Uri uri)
		{
			HttpProxy.Trace.TraceInformation($"Connecting with server {uri.DnsSafeHost}");
			var ipAddresses = await DnsResolver.GetHostAddressesAsync(uri.DnsSafeHost);

			foreach (var ipAddress in ipAddresses)
			{
				Connection connection = null;
				try
				{
					connection = new Connection(new IPEndPoint(ipAddress, uri.Port));
					await connection.ConnectAsync();
					return connection;
				}
				catch (SocketException)
				{
					connection?.Close();
				}
			}
			return null;
		}

		public async Task CreateHttpsTunnelAsync()
		{
			_session.Trace.TraceInformation("Creating Server Tunnel");
			var sslStream = new SslStream(_session.ServerPipe.Stream, false);
			await sslStream.AuthenticateAsClientAsync(_session.Request.Uri.Host, null, SslProtocols.Default, false);

			_session.Trace.TraceInformation("Authenticated as client!");
			_session.ServerPipe = new Pipe(sslStream);
		}

		public async Task NegociateTunnelWithProxyAsync()
		{
			_session.Trace.TraceInformation("Negociating tunnel with proxy server");
			var connectRequest = new Request(
				_session.Request.RequestLine,
				new HttpHeaders{
					{"Host", _session.Request.Headers.Host },
					{"Connection", "Keep-Alive" }
				});

			await SendRequestAsync(connectRequest);
			var response = await InternalReceiveResponseAsync();

			if (response.StatusLine.Code == "200" && 
				response.StatusLine.Description.Equals("connection established", StringComparison.OrdinalIgnoreCase))
			{
				_session.Trace.TraceInformation("Tunnel established with proxy");
			}
			else
			{
				_session.Trace.TraceInformation("Tunnel with proxy failed");
			}
		}

		public async Task ResendRequestAsync()
		{
			await SendRequestAsync(_session.Request);
		}

		internal async Task SendRequestAsync(Request request)
		{
			using (new TraceScope(_session.Trace, "Sending request to server"))
			{
				await _pipe.Writer.WriteRequestLineAsync(_session.Request.RequestLine);
				await _pipe.Writer.WriteHeadersAsync(_session.Request.Headers.TransformHeaders());
				await _pipe.Writer.WriteBodyAsync(_session.Request.Body);
			}
		}

		public async Task ReceiveResponseAsync()
		{
			_session.Response = await InternalReceiveResponseAsync();
		}

		internal async Task<Response> InternalReceiveResponseAsync()
		{
			_session.Trace.TraceInformation("Receiving response from server");
			var statusLine = await _pipe.Reader.ReadStatusLineAsync();
			if (statusLine == null)
			{
				_session.Trace.TraceInformation("No status line received.");
				return null;
			}

			_session.Trace.TraceEvent(TraceEventType.Verbose, 0, statusLine.ToString());
			_session.Trace.TraceInformation("Receiving request headers");

			var headers = await _pipe.Reader.ReadHeadersAsync();
			_session.Trace.TraceData(TraceEventType.Verbose, 0, headers.ToString());

			var response = new Response(statusLine, headers);

			if (response.HasBody)
			{
				if (response.IsChunked)
				{
					_session.Trace.TraceEvent(TraceEventType.Verbose, 0, "Receiving chuncked body");
					response.Body = await _pipe.Reader.ReadChunckedBodyAsync();
					response.Headers.Remove("Transfer-Encoding");
				}
				else if (response.Headers.ContentLength.HasValue && response.Headers.ContentLength.Value >0 )
				{
					_session.Trace.TraceEvent(TraceEventType.Verbose, 0, $"Receiving body with content length = {response.Headers.ContentLength}");
					response.Body = await _pipe.Reader.ReadBodyAsync(response.Headers.ContentLength.Value);
				}
				else
				{
					_session.Trace.TraceEvent(TraceEventType.Verbose, 0, "Receiving body with no content length");
					response.Body = await _pipe.Reader.ReadBodyToEndAsync();
				}
			}

			return response;
		}

		public void Close()
		{
			_session.Trace.TraceEvent(TraceEventType.Verbose, 0, "Closing server handler");
			_pipe.Close();
		}
	}

	internal static class DnsResolver
	{
		private static readonly Dictionary<string, IPAddress[]> Cache = new Dictionary<string, IPAddress[]>();
		private static readonly object LockObj = new object();

		public static async Task<IPAddress[]> GetHostAddressesAsync(string dnsSafeHost)
		{
			if (!Cache.ContainsKey(dnsSafeHost))
			{
				var ipAddresses = await Task<IPAddress[]>.Factory.FromAsync(
					Dns.BeginGetHostAddresses,
					Dns.EndGetHostAddresses,
					dnsSafeHost, null);

				lock (LockObj)
				{
					if (Cache.ContainsKey(dnsSafeHost))
					{
						return Cache[dnsSafeHost];
					}
					Cache[dnsSafeHost] = ipAddresses;
				}
				HttpProxy.Trace.TraceEvent(TraceEventType.Verbose, 0, $"DNS lookup done and cached: {ipAddresses[0]}");
			}
			else
			{
				HttpProxy.Trace.TraceEvent(TraceEventType.Verbose, 0, "DNS lookup resolved from proxy");
			}

			return Cache[dnsSafeHost];
		}
	}
}