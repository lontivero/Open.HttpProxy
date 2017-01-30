using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Open.HttpProxy.Utils;

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

		private async Task<int> ReadBytesAsync(byte[] buffer, int index, int count)
		{
			int num = 0;
			int num2;
			do
			{
				num2 = await _stream.ReadAsync(buffer, index + num, count - num).WithoutCapturingContext();
				num += num2;
			}
			while (num2 > 0 && num < count);
			return num;
		}

		public async Task<RequestLine> ReadRequestLineAsync()
		{
			var line = await ReadLineAsync().WithoutCapturingContext();
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
			var line = await ReadLineAsync().WithoutCapturingContext();
			while (!string.IsNullOrEmpty(line))
			{
				headers.AddLine(line);
				line = await ReadLineAsync().WithoutCapturingContext();
			}
			return headers;
		}

		public async Task<string> ReadLineAsync()
		{
			if (_currentLine == null)
				_currentLine = new StringBuilder(128);

			while (_lineState != LineState.Lf)
			{
				var b = await ReadByteAsync().WithoutCapturingContext();
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
			if (contentLength > 0)
			{
				var buffer = HttpProxy.BufferAllocator.AllocateAsync(contentLength);
				var readed = await ReadBytesAsync(buffer.Array, buffer.Offset, contentLength).WithoutCapturingContext();
				var arr = new byte[readed];
				Buffer.BlockCopy(buffer.Array, buffer.Offset, arr, 0, readed);
				return arr;
			}
			return new byte[0];
		}

		public async Task<byte[]> ReadBodyToEndAsync()
		{
			var buffer = HttpProxy.BufferAllocator.AllocateAsync(200*1024);
			var readPos = 0;
			int readed;
			do
			{
				readed = await ReadBytesAsync(buffer.Array, readPos, 1024).WithoutCapturingContext();
				readPos += readed;
			} while (readed > 0);
			var arr = new byte[readPos];
			Buffer.BlockCopy(buffer.Array, buffer.Offset, arr, 0, readPos);
			return arr;
		}

		public async Task<byte[]> ReadChunckedBodyAsync()
		{
			var buffer = HttpProxy.BufferAllocator.AllocateAsync(200 * 1024);
			var readPos = 0;
			int chunkSize;
			do
			{
				var chuchkHead = await ReadLineAsync().WithoutCapturingContext();
				chunkSize = int.Parse(chuchkHead, NumberStyles.HexNumber);

				if (chunkSize > 0)
				{
					var readed = await ReadBytesAsync(buffer.Array, buffer.Offset + readPos, chunkSize).WithoutCapturingContext();
					readPos += readed;
				}
				await ReadLineAsync().WithoutCapturingContext();
			} while (chunkSize > 0);
			var arr = new byte[readPos];
			Buffer.BlockCopy(buffer.Array, buffer.Offset, arr, 0, readPos);
			return arr;
		}

		private async Task<int> ReadByteAsync()
		{
			// TODO: a new char everytime? this seems crazy!
			var array = new byte[1];
			if (await _stream.ReadAsync(array, 0, 1).WithoutCapturingContext() == 0)
			{
				return -1;
			}
			return array[0];
		}
	}
}