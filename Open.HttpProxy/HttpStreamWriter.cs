using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	internal class HttpStreamWriter
	{
		private readonly Stream _stream;
		private readonly Encoding _encoder;
		private static readonly byte[] NewLine = {0x0a, 0x0d};

		public HttpStreamWriter(Stream stream)
		{
			_stream = stream;
			_encoder = new ASCIIEncoding();
		}

		public async Task WriteRequestLineAsync(RequestLine requestLine)
		{
			await WriteLineAsync(requestLine.ToString2());
		}
		public async Task WriteStatusLineAsync(StatusLine statusLine)
		{
			await WriteLineAsync(statusLine.ToString());
		}

		public async Task WriteHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
		{
			foreach (var header in headers)
			{
				await WriteLineAsync($"{header.Key}: {header.Value}");
			}
			await WriteLineAsync();
			await _stream.FlushAsync();
		}

		public async Task WriteBodyAsync(byte[] body)
		{
			if (body != null && body.Length > 0)
			{
				await _stream.WriteAsync(body, 0, body.Length);
			}
			try
			{
				await _stream.FlushAsync();
			}
			catch
			{
			}
		}

		private async Task WriteLineAsync(string str=null)
		{
			str = str + "\r\n"; 
			var buffer = _encoder.GetBytes(str);
			await _stream.WriteAsync(buffer, 0, buffer.Length);
		}

		public void Close()
		{
			//_stream.Close();
		}
	}
}