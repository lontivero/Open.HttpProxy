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
			_session.Trace.TraceInformation("Receiving request line");

			var requestLine = await _pipe.Reader.ReadRequestLineAsync();
			if (requestLine == null)
			{
				_session.Trace.TraceInformation("No request line received.");
				return;
			}
			_session.Trace.TraceEvent(TraceEventType.Verbose, 0, requestLine.ToString());
			_session.Trace.TraceInformation("Receiving request headers");

			var headers = await _pipe.Reader.ReadHeadersAsync();
			_session.Trace.TraceData(TraceEventType.Verbose, 0, headers.ToString());

			_session.Request = new Request(requestLine, headers);
		}

		public async Task ReceiveBodyAsync()
		{
			_session.Trace.TraceInformation("Receiving request body");

			var requestLine = _session.Request.RequestLine;
			if (requestLine.IsVerb("POST") || requestLine.IsVerb("PUT"))
			{
				_session.Request.Body = _session.Request.IsChunked
					? await _pipe.Reader.ReadChunckedBodyAsync()
					: await _pipe.Reader.ReadBodyAsync(_session.Request.Headers.ContentLength.Value);
			}
		}

		public async Task CreateHttpsTunnelAsync()
		{
			var requestLine = _session.Request.RequestLine;
			_session.Trace.TraceInformation($"Creating Client Tunnel for {_session.Request.EndPoint.Host}");

			await BuildAndReturnResponseAsync(requestLine.Version, 200, "Connection established");
			var cert = await CertificateProvider.Default.GetCertificateForSubjectAsync(_session.Request.EndPoint.WildcardDomain);
			var sslStream = new SslStream(_session.ClientPipe.Stream, false);
			await sslStream.AuthenticateAsServerAsync(cert, false, SslProtocols.Default, true);

			_session.Trace.TraceInformation("Authenticated as server!");

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
			await BuildAndReturnResponseAsync(version, code, description, nbody, true);
		}

		public async Task BuildAndReturnResponseAsync(ProtocolVersion version, int code, string description, string body = null, bool closeConnection = false)
		{
			using (new TraceScope(_session.Trace, "Creating Client Tunnel"))
			{
				var statusLine = new StatusLine(version, code.ToString(), description);
				_session.Trace.TraceInformation($"Responding with [{statusLine}]");
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
				await ReturnResponse();
			}
		}

		public async Task ResendResponseAsync()
		{
			_session.Trace.TraceInformation("Sending server response back to the client");
			await _pipe.Writer.WriteStatusLineAsync(_session.Response.StatusLine);
			await _pipe.Writer.WriteHeadersAsync(_session.Response.Headers);
			await _pipe.Writer.WriteBodyAsync(_session.Response.Body);
		}

		internal async Task ReturnResponse()
		{
			var writer = _pipe.Writer;
			await writer.WriteStatusLineAsync(_session.Response.StatusLine);
			await writer.WriteHeadersAsync(_session.Response.Headers);
			await writer.WriteBodyAsync(_session.Response.Body);
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
			_session.Trace.TraceEvent(TraceEventType.Verbose, 0, "Closing client handler");
			_pipe.Close();
		}
	}
}