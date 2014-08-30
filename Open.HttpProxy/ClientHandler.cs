using System;
using System.IO;
using System.Threading.Tasks;
using Open.Tcp;
using Open.Tcp.BufferManager;

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
        LF,
        CR
    }

    internal class ClientHandler
    {
        private readonly Session _session;
        private readonly Connection _connection;
        private InputState _inputState;
        private Stream _stream;
        private HttpStreamReader _reader;

        public ClientHandler(Session session, Connection clientConnection)
        {
            _session = session;
            _connection = clientConnection;
//            _stream = new BufferedStream(new ConnectionStream(_connection));
            _stream = new ManualBufferedStream(new ConnectionStream(_connection), _session.BufferAllocator);
            _reader = new HttpStreamReader(_stream);
        }

        public async Task ReceiveEntityAsync()
        {
            while (! await IsRequestComplete(_reader));
        }

        public async Task<Stream> GetRequestStreamAsync(int contentLenght)
        {
            var result = new MemoryStream();
            var writer = new StreamWriter(result);
            var b = new char[contentLenght];
            await _reader.ReadBlockAsync(b, 0, contentLenght);
            await writer.WriteAsync(b);
            await writer.FlushAsync();
            result.Seek(0, SeekOrigin.Begin);
            return result;
        }

        private async Task<bool> IsRequestComplete(TextReader reader)
        {
            string line;

            try {
                line = await reader.ReadLineAsync();
            } catch {
                _session.ErrorMessage = "Bad request";
                _session.ErrorStatus = 400;
                return true;
            }

            do {
                if (line == null)
                    break;
                if (line == "") {
                    if (_inputState == InputState.RequestLine)
                        continue;
                    return true;
                }

                if (_inputState == InputState.RequestLine) {
                    _session.Request.SetRequestLine (line);
                    _inputState = InputState.Headers;
                } else {
                    try {
                        _session.Request.AddHeader (line);
                    } catch (Exception e) {
                        _session.ErrorMessage = e.Message;
                        _session.ErrorStatus = 400;
                        return true;
                    }
                }

                if (_session.HaveError)
                    return true;

                try {
                    line = await reader.ReadLineAsync();
                } catch {
                    _session.ErrorMessage = "Bad request";
                    _session.ErrorStatus = 400;
                    return true;
                }
            } while (line != null);

            return false;
        }

        internal void BuildAndReturnResponse(int statusCode, string description)
        {
            _session.Response.Headers = new HTTPResponseHeaders();
            _session.Response.StatusCode = statusCode;
            _session.Response.StatusCodeDescription = description;
            _session.Response.Headers.Add("Date", DateTime.UtcNow.ToString("r"));
            _session.Response.Headers.Add("Content-Type", "text/html; charset=UTF-8");
            _session.Response.Headers.Add("Connection", "close");
            _session.Response.Headers.Add("Timestamp", DateTime.UtcNow.ToString("HH:mm:ss.fff"));
            _session.ReturnResponse();
        }

    }
}