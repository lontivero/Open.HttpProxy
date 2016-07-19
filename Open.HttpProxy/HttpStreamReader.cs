using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	internal class HttpStreamReader : StreamReader
	{
		private StringBuilder _currentLine;
		private LineState _lineState;

		public HttpStreamReader(Stream stream)
			: base(stream)
		{
		}

		public override async Task<int> ReadBlockAsync(char[] buffer, int index, int count)
		{
			int num = 0;
			int num2;
			do
			{
				num2 = await ReadAsync(buffer, index + num, count - num).ConfigureAwait(false);
				num += num2;
			}
			while (num2 > 0 && num < count);
			return num;
		}

		public override async Task<string> ReadLineAsync()
		{
			if (_currentLine == null)
				_currentLine = new StringBuilder(128);


			while (_lineState != LineState.Lf)
			{
				var b = await ReadByteAsync();
				switch (b)
				{
					case -1:
						throw new Exception();
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

		private async Task<int> ReadByteAsync()
		{
			// TODO: a new char everytime? this seems crazy!
			var array = new char[1];
			if (await ReadAsync(array, 0, 1) == 0)
			{
				return -1;
			}
			return array[0];
		}
	}
}