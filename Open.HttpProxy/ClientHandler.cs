using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	using BufferManager;

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
		private readonly ConnectionStream _connectionStream;
		private Pipe _pipe;

		public ClientHandler(Session session, Connection clientConnection)
		{
			_session = session;
			_connectionStream = new ConnectionStream(clientConnection);
//			_pipe = new Pipe(new BufferedStream(_connectionStream));
			_pipe = new Pipe(new BufferedStream(_connectionStream));
		}

		public async Task ReceiveAsync()
		{
			await _pipe.StartAsync();
			var parser = new RequestParser(_pipe.Reader);
			var request = await parser.ParseAsync();
			if (request.IsHttps)
			{
				await BuildAndReturnResponseAsync(request.RequestLine.Version, 200, "Connection established");
				_pipe = new SslPipe(_connectionStream, request.RequestLine.EndPoint.Host);
				await _pipe.StartAsync();
				parser = new RequestParser(_pipe.Reader);
				request = await parser.ParseAsync();
			}
			_session.Request = request;
		}

		public async Task ReceiveBodyAsync()
		{
			var verb = _session.Request.RequestLine.Verb;
			if (verb.Equals("POST", StringComparison.OrdinalIgnoreCase) || verb.Equals("PUT", StringComparison.OrdinalIgnoreCase))
			{
				_session.Request.Body = _session.Request.IsChunked
					? await _pipe.Reader.ReadChunckedBodyAsync()
					: await _pipe.Reader.ReadBodyAsync(_session.Request.Headers.ContentLength.Value);
			}
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


		public async Task BuildAndReturnResponseAsync(ProtocolVersion version, int code, string description)
		{
			_session.HasError = true;
			_session.Response.StatusLine = new StatusLine(version, code.ToString(), description);
			_session.Response.Headers.Add("Date", DateTime.UtcNow.ToString("r"));
			_session.Response.Headers.Add("Content-Type", "text/html; charset=UTF-8");
			_session.Response.Headers.Add("Connection", "close");
			_session.Response.Headers.Add("Timestamp", DateTime.UtcNow.ToString("HH:mm:ss.fff"));
			await ReturnResponse();
		}

		public async Task SendEntityAsync()
		{
			await _pipe.Writer.WriteStatusLineAsync(_session.Response.StatusLine);
			await _pipe.Writer.WriteHeadersAsync(_session.Response.Headers);
		}

		public async Task SendBodyAsync()
		{
			await _pipe.Writer.WriteBodyAsync(_session.Response.Body);
		}

		internal async Task ReturnResponse()
		{
			var writer = _pipe.Writer;
			await writer.WriteStatusLineAsync(_session.Response.StatusLine);
			await writer.WriteHeadersAsync(_session.Response.Headers);
			await writer.WriteBodyAsync(_session.Response.Body);
		}
	}
}