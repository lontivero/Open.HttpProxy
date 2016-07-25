using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	internal class ResponseParser
	{
		private readonly HttpStreamReader _reader;
		private InputState _inputState;

		public ResponseParser(HttpStreamReader reader)
		{
			_reader = reader;
			_inputState = InputState.RequestLine;
		}

		public async Task<Response> ParseAsync()
		{
			var response = new Response();
			var line = await _reader.ReadLineAsync();
			while (line != null)
			{
				if (line == string.Empty)
				{
					if (_inputState != InputState.RequestLine)
						return response;

					line = await _reader.ReadLineAsync();
					continue;
				}

				if (_inputState == InputState.RequestLine)
				{
					response.StatusLine = StatusLine.Parse(line);
					_inputState = InputState.Headers;
				}
				else
				{
					response.Headers.AddLine(line);
				}

				line = await _reader.ReadLineAsync();
			}
			return response;
		}
	}
}