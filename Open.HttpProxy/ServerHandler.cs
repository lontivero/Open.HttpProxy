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
using Open.HttpProxy.Utils;

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

		public static async Task<Socket> ConnectToHostAsync(Uri uri)
		{
			HttpProxy.Logger.Info($"Connecting with server {uri.DnsSafeHost}");
			var ipAddresses = await DnsResolver.GetHostAddressesAsync(uri.DnsSafeHost).WithoutCapturingContext();

			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			foreach (var ipAddress in ipAddresses)
			{
				try
				{
					await Task.Factory.FromAsync(
						socket.BeginConnect, 
						socket.EndConnect,
						new IPEndPoint(ipAddress, uri.Port), socket)
						.WithoutCapturingContext();
					return socket;
				}
				catch (SocketException)
				{
					socket?.Close();
				}
			}
			return null;
		}

		public async Task CreateHttpsTunnelAsync()
		{
			_session.Logger.Info("Creating Server Tunnel");
			var sslStream = new SslStream(_session.ServerPipe.Stream, false);
			await sslStream.AuthenticateAsClientAsync(_session.Request.Uri.Host, null, SslProtocols.Default, false).WithoutCapturingContext();

			_session.Logger.Info("Authenticated as client");
			_session.ServerPipe = new Pipe(sslStream);
		}

		public async Task NegociateTunnelWithProxyAsync()
		{
			_session.Logger.Info("Negociating tunnel with proxy server");
			var connectRequest = new Request(
				_session.Request.RequestLine,
				new HttpHeaders{
					{"Host", _session.Request.Headers.Host },
					{"Connection", "keep-alive" }
				});

			await SendRequestAsync(connectRequest).WithoutCapturingContext();
			var response = await InternalReceiveResponseAsync().WithoutCapturingContext();

			if (response.StatusLine.Code == "200" && 
				response.StatusLine.Description.Equals("connection established", StringComparison.OrdinalIgnoreCase))
			{
				_session.Logger.Info("Tunnel established with proxy");
			}
			else
			{
				_session.Logger.Info("Tunnel with proxy failed");
			}
		}

		public async Task ResendRequestAsync()
		{
			await SendRequestAsync(_session.Request).WithoutCapturingContext();
		}

		internal async Task SendRequestAsync(Request request)
		{
			using (_session.Logger.Enter("Sending request to server"))
			{
				await _pipe.Writer.WriteRequestLineAsync(_session.Request.RequestLine).WithoutCapturingContext();
				await _pipe.Writer.WriteHeadersAsync(_session.Request.Headers.TransformHeaders()).WithoutCapturingContext();
				await _pipe.Writer.WriteBodyAsync(_session.Request.Body).WithoutCapturingContext();
			}
		}

		public async Task ReceiveResponseAsync()
		{
			_session.Response = await InternalReceiveResponseAsync().WithoutCapturingContext();
		}

		internal async Task<Response> InternalReceiveResponseAsync()
		{
			_session.Logger.Info("Receiving response from server");
			var statusLine = await _pipe.Reader.ReadStatusLineAsync();
			if (statusLine == null)
			{
				_session.Logger.Info("No status line received.");
				return null;
			}

			_session.Logger.Verbose(statusLine.ToString());
			_session.Logger.Info("Receiving request headers");

			var headers = await _pipe.Reader.ReadHeadersAsync().WithoutCapturingContext();
			_session.Logger.Verbose(headers.ToString());

			var response = new Response(statusLine, headers);

			if (response.HasBody)
			{
				if (response.IsChunked)
				{
					_session.Logger.Verbose("Receiving chuncked body");
					response.Body = await _pipe.Reader.ReadChunckedBodyAsync().WithoutCapturingContext();
					response.Headers.Remove("Transfer-Encoding");
				}
				else if (response.Headers.ContentLength.HasValue && response.Headers.ContentLength.Value >0 )
				{
					_session.Logger.Verbose($"Receiving body with content length = {response.Headers.ContentLength}");
					response.Body = await _pipe.Reader.ReadBodyAsync(response.Headers.ContentLength.Value).WithoutCapturingContext();
				}
				else
				{
					_session.Logger.Verbose("Receiving body with no content length");
					response.Body = await _pipe.Reader.ReadBodyToEndAsync().WithoutCapturingContext();
				}
			}

			return response;
		}

		public void Close()
		{
			_session.Logger.Verbose("Closing server handler");
			//_pipe.Close();
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
				HttpProxy.Logger.Verbose($"DNS lookup done and cached: {ipAddresses[0]}");
			}
			else
			{
				HttpProxy.Logger.Verbose("DNS lookup resolved from proxy");
			}

			return Cache[dnsSafeHost];
		}
	}
}