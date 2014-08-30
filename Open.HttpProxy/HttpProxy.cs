using System.Threading.Tasks;
using Open.Tcp;
using Open.Tcp.BufferManager;
using Open.Tcp.EventArgs;

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
        }

        public void Start()
        {
            _listener.Start();
        }
    }
}
