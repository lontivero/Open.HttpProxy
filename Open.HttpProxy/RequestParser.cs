using System.IO;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	internal class RequestParser
	{
		private readonly HttpStreamReader _reader;
		private InputState _inputState;

		public RequestParser(HttpStreamReader reader)
		{
			_reader = reader;
			_inputState = InputState.RequestLine;
		}

		public async Task<Request> ParseAsync()
		{
			var request = new Request();
			var line = await _reader.ReadLineAsync();

			while (line != null)
			{
				if (line == string.Empty)
				{
					if (_inputState != InputState.RequestLine) 
						return request;

					line = await _reader.ReadLineAsync();
					continue;
				}

				if (_inputState == InputState.RequestLine)
				{
					request.RequestLine = new RequestLine(line);
					_inputState = InputState.Headers;
				}
				else
				{
					request.Headers.AddLine(line);
				}

				line = await _reader.ReadLineAsync();
			}
			return request;
		}
	}
}