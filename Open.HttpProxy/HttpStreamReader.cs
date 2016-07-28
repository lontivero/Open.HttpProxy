using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	internal class HttpStreamReader 
	{
		private readonly Stream _stream;
		private StringBuilder _currentLine;
		private LineState _lineState;
		 
		public HttpStreamReader(Stream stream)
		{
			_stream = stream;
		}

		public async Task<int> ReadBytesAsync(byte[] buffer, int index, int count)
		{
			int num = 0;
			int num2;
			do
			{
				num2 = await _stream.ReadAsync(buffer, index + num, count - num);
				num += num2;
			}
			while (num2 > 0 && num < count);
			return num;
		}

		public async Task<RequestLine> ReadRequestLineAsync()
		{
			var line = await ReadLineAsync();
			return line==null ? null : RequestLine.Parse(line);
		}

		public async Task<StatusLine> ReadStatusLineAsync()
		{
			var line = await ReadLineAsync();
			return line == null ? null : StatusLine.Parse(line);
		}

		public async Task<HttpHeaders> ReadHeadersAsync()
		{
			var headers = new HttpHeaders();
			var line = await ReadLineAsync();
			while (!string.IsNullOrEmpty(line))
			{
				headers.AddLine(line);
				line = await ReadLineAsync();
			}
			return headers;
		}

		public async Task<string> ReadLineAsync()
		{
			if (_currentLine == null)
				_currentLine = new StringBuilder(128);


			while (_lineState != LineState.Lf)
			{
				var b = await ReadByteAsync();
				if (b == -1)
					break;
				switch (b)
				{
					case 13:
						_lineState = LineState.Cr;
						break;
					case 10:
						_lineState = LineState.Lf;
						break;
					default:
						_currentLine.Append((char)b);
						break;
				}
			}

			if (_lineState != LineState.Lf) return null;

			_lineState = LineState.None;
			var result = _currentLine.ToString();
			_currentLine.Length = 0;

			return result;
		}

		public async Task<byte[]> ReadBodyAsync(int contentLength)
		{
			var buffer = new byte[contentLength];
			await ReadBytesAsync(buffer, 0, buffer.Length);
			return buffer;
		}

		public async Task<byte[]> ReadBodyToEndAsync()
		{
			var mem = new MemoryStream(200 * 1024);
			var buffer = new byte[4 * 1024];
			int readed;
			do
			{
				readed = await ReadBytesAsync(buffer, 0, buffer.Length);
				await mem.WriteAsync(buffer, 0, readed);
			} while (readed > 0);
			mem.Seek(0, SeekOrigin.Begin);
			return mem.ToArray();
		}

		public async Task<byte[]> ReadChunckedBodyAsync()
		{
			var mem = new MemoryStream(200*1024);
			int chunkSize;
			do
			{
				var chuchkHead = await ReadLineAsync();
				chunkSize = int.Parse(chuchkHead, NumberStyles.HexNumber);

				if (chunkSize > 0)
				{
					var buffer = new byte[chunkSize];
					var xx = await ReadBytesAsync(buffer, 0, chunkSize);
					await mem.WriteAsync(buffer, 0, buffer.Length);
				}
				await ReadLineAsync();
			} while (chunkSize > 0);
			mem.Seek(0, SeekOrigin.Begin);
			return mem.ToArray();
		}

		private async Task<int> ReadByteAsync()
		{
			
			// TODO: a new char everytime? this seems crazy!
			var array = new byte[1];
			if (await _stream.ReadAsync(array, 0, 1) == 0)
			{
				return -1;
			}
			return array[0];
		}

		public void Close()
		{
			//_stream.Close();
		}
	}
}