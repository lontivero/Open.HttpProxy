using Open.HttpProxy.BufferManager;
using Open.HttpProxy.EventArgs;
using Open.HttpProxy.Listeners;

namespace Open.HttpProxy
{
    public class HttpProxy
    {
        private readonly TcpListener _listener;
        private readonly BufferAllocator _bufferAllocator;
        private readonly int _port = 8888;

        public HttpProxy()
        {
            _listener = new TcpListener(_port);
            _bufferAllocator = new BufferAllocator(new byte[1024*1024]);
            _listener.ConnectionRequested += OnConnectionRequested;
        }

        private async void OnConnectionRequested(object sender, ConnectionEventArgs e)
        {
            var session = new Session(e.Connection, _bufferAllocator);
            await session.ReceiveRequestAsync();
            await session.ResendRequestAsync();
            //await session.ClientHandler.BuildAndReturnResponseAsync(404, "Not Found");
        }

        public void Start()
        {
            _listener.Start();
        }
    }
}
