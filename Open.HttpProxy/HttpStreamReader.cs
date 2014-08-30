using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
    class HttpStreamReader : StreamReader
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


            while (_lineState != LineState.LF)
            {
                var b = await ReadByteAsync();
                if (b == 13)
                {
                    _lineState = LineState.CR;
                }
                else if (b == 10)
                {
                    _lineState = LineState.LF;
                }
                else
                {
                    _currentLine.Append((char)b);
                }
            }

            string result = null;
            if (_lineState == LineState.LF)
            {
                _lineState = LineState.None;
                result = _currentLine.ToString();
                _currentLine.Length = 0;
            }

            return result;
        }

        private async Task<int> ReadByteAsync()
        {
            var array = new char[1];
            if (await ReadAsync(array, 0, 1) == 0)
            {
                return -1;
            }
            return (int)array[0];
        }
    }
}