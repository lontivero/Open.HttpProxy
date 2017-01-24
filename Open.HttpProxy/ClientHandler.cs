using System;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading.Tasks;
using Open.HttpProxy.Utils;

namespace Open.HttpProxy
{
	internal enum InputState
	{
		RequestLine,
		Headers,
	}

	internal enum LineState
	{
		None,
		Lf,
		Cr
	}

	internal class ClientHandler
	{
		private readonly Session _session;
		private Pipe _pipe => _session.ClientPipe;

		public ClientHandler(Session session)
		{
			_session = session;
		}

		public async Task ReceiveAsync()
		{
			_session.Logger.Info("Receiving request line");

			var requestLine = await _pipe.Reader.ReadRequestLineAsync().WithoutCapturingContext();
			if (requestLine == null)
			{
				_session.Logger.Info("No request line received.");
				return;
			}
			_session.Logger.Verbose(requestLine.ToString());
			_session.Logger.Info("Receiving request headers");

			var headers = await _pipe.Reader.ReadHeadersAsync().WithoutCapturingContext();
			_session.Logger.LogData(TraceEventType.Verbose, headers.ToString());

			_session.Request = new Request(requestLine, headers);
		}

		public async Task ReceiveBodyAsync()
		{
			_session.Logger.Info("Receiving request body");

			var requestLine = _session.Request.RequestLine;
			if (requestLine.IsVerb("POST") || requestLine.IsVerb("PUT"))
			{
				_session.Request.Body = _session.Request.IsChunked
					? await _pipe.Reader.ReadChunckedBodyAsync().WithoutCapturingContext()
					: await _pipe.Reader.ReadBodyAsync(_session.Request.Headers.ContentLength.Value).WithoutCapturingContext();
			}
		}

		public async Task CreateHttpsTunnelAsync()
		{
			var requestLine = _session.Request.RequestLine;
			_session.Logger.Info($"Creating Client Tunnel for {_session.Request.EndPoint.Host}");

			await BuildAndReturnResponseAsync(requestLine.Version, 200, "Connection established").WithoutCapturingContext();
			var cert = await CertificateProvider.Default.GetCertificateForSubjectAsync(_session.Request.EndPoint.WildcardDomain).WithoutCapturingContext();
			var sslStream = new SslStream(_session.ClientPipe.Stream, false);
			await sslStream.AuthenticateAsServerAsync(cert, false, SslProtocols.Default, true).WithoutCapturingContext();

			_session.Logger.Info("Authenticated as server!");
			_session.ClientPipe = new Pipe(sslStream);
		}

		public async Task SendErrorAsync(ProtocolVersion version, int code, string description, string body )
		{
			var nbody = $@"<html>
				<head><title>{code} - {description}</title></head>
				<body>
					<h1>{code} - {description}</h1>
					<pre>{Html.Encode(body)}</pre>
				</body>
			</html>";
			await BuildAndReturnResponseAsync(version, code, description, nbody, true).WithoutCapturingContext();
		}

		public async Task BuildAndReturnResponseAsync(ProtocolVersion version, int code, string description, string body = null, bool closeConnection = false)
		{
			using (_session.Logger.Enter("Creating Client Tunnel"))
			{
				var statusLine = new StatusLine(version, code.ToString(), description);
				_session.Logger.Info($"Responding with [{statusLine}]");
				_session.Response = new Response(
					statusLine,
					new HttpHeaders
					{
						{"Date", DateTime.UtcNow.ToString("r")},
						{"Timestamp", DateTime.UtcNow.ToString("HH:mm:ss.fff")}
					});
				if (closeConnection)
				{
					_session.Response.Headers.Add("Connection", "close");
				}
				if (body != null)
				{
					_session.Response.Body = _session.Response.BodyEncoding.GetBytes(body);
				}
				await ReturnResponse().WithoutCapturingContext();
			}
		}

		public async Task ResendResponseAsync()
		{
			_session.Logger.Info("Sending server response back to the client");
			await _pipe.Writer.WriteStatusLineAsync(_session.Response.StatusLine).WithoutCapturingContext();
			await _pipe.Writer.WriteHeadersAsync(_session.Response.Headers).WithoutCapturingContext();
			await _pipe.Writer.WriteBodyAsync(_session.Response.Body).WithoutCapturingContext();
		}

		internal async Task ReturnResponse()
		{
			if(!_pipe.Stream.CanWrite) return;
			var writer = _pipe.Writer;
			await writer.WriteStatusLineAsync(_session.Response.StatusLine).WithoutCapturingContext();
			await writer.WriteHeadersAsync(_session.Response.Headers).WithoutCapturingContext();
			await writer.WriteBodyAsync(_session.Response.Body).WithoutCapturingContext();
		}


		//internal async Task<Stream> GetRequestStreamAsync(int contentLenght)
		//{
		//	var result = new MemoryStream();
		//	var writer = new StreamWriter(result);
		//	var b = new char[contentLenght];
		//	await _pipe.Reader.ReadBlockAsync(b, 0, contentLenght);
		//	await writer.WriteAsync(b);
		//	await writer.FlushAsync();
		//	result.Seek(0, SeekOrigin.Begin);
		//	return result;
		//}

		public void Close()
		{
			_session.Logger.Verbose("Closing client handler");
			//_pipe.Close();
		}
	}
}