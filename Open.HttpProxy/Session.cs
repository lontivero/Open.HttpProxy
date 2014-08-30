using System.IO;
using System.Threading.Tasks;
using Open.Tcp;
using Open.Tcp.BufferManager;

namespace Open.HttpProxy
{
    internal class Session
    {
        private readonly Connection _clientConnection;
        private readonly BufferAllocator _bufferAllocator;
        private readonly Request _request;
        private readonly Response _response;
        private readonly ClientHandler _clientHandler;

        public Session(Connection clientConnection, BufferAllocator bufferAllocator)
        {
            _clientConnection = clientConnection;
            _bufferAllocator = bufferAllocator;
            _request = new Request(this);
            _response = new Response(this);
            _clientHandler = new ClientHandler(this, _clientConnection);
        }

        public BufferAllocator BufferAllocator
        {
            get { return _bufferAllocator; }
        }

        public async Task ReceiveRequestAsync()
        {
            await ClientHandler.ReceiveEntityAsync();
            var s = await Request.GetContentStreamAsync();
            var sr = new StreamReader(s);
            var body = await sr.ReadToEndAsync();
        }

        public string ErrorMessage { get; set; }
        public int ErrorStatus { get; set; }

        public Request Request
        {
            get { return _request; }
        }

        public Response Response
        {
            get { return _response; }
        }

        internal bool HaveError { get; set; }

        internal ClientHandler ClientHandler
        {
            get { return _clientHandler; }
        }

        internal async Task ReturnResponse()
        {
            var response = Response.ToByteArray();
            await _clientConnection.SendAsync(response, 0, response.Length);
        }
    }
}