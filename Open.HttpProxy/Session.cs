using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Open.HttpProxy.BufferManager;

namespace Open.HttpProxy
{
    internal class Session
    {
        private readonly Connection _clientConnection;
        private readonly BufferAllocator _bufferAllocator;
        private readonly Request _request;
        private readonly Response _response;
        private readonly ClientHandler _clientHandler;
        private readonly ServerHandler _serverHandler;

        public Session(Connection clientConnection, BufferAllocator bufferAllocator)
        {
            _clientConnection = clientConnection;
            _bufferAllocator = bufferAllocator;
            _request = new Request(this);
            _response = new Response(this);
            _clientHandler = new ClientHandler(this, _clientConnection);
            _serverHandler = new ServerHandler(this);
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
            var stream = new BufferedStream(new ConnectionStream(_clientConnection));
            var writer = new StreamWriter(stream);
            await writer.WriteAsync(_response.StatusLine.ResponseLine.ToCharArray());
            await writer.WriteAsync("\r\n".ToCharArray());
            await writer.WriteAsync(_response.Headers.ToCharArray());
            await writer.WriteAsync(_response.Body.ToCharArray());
            await writer.FlushAsync();
            stream.Close();
        }

        public async Task ResendRequestAsync()
        {
            await _serverHandler.ConnectToHostAsync();

        }
    }

    internal class ServerHandler
    {
        private readonly Session _session;
        private Connection _connection;

        public ServerHandler(Session session)
        {
            _session = session;
        }

        public async Task ConnectToHostAsync()
        {
            var uri = GetUriFromRequest();
            var dnsEndPoint = new DnsEndPoint(uri.DnsSafeHost, uri.Port);
            var ipAddresses = await Task<IPAddress[]>.Factory.FromAsync(
                            Dns.BeginGetHostAddresses,
                            Dns.EndGetHostAddresses,
                            uri.DnsSafeHost, null);

            foreach (var ipAddress in ipAddresses)
            {
                try
                {
                    _connection = new Connection(new IPEndPoint(ipAddress, uri.Port));
                    await _connection.ConnectAsync();
                    break;
                }
                catch
                {
                    _connection.Close();    
                }
            }

        }

        private Uri GetUriFromRequest()
        {
            var requestUri = _session.Request.RequestLine.Uri;
            var requestHost = _session.Request.Headers.Host;
            if (requestUri == "*")
            {
                return new Uri(requestHost, UriKind.Relative);
            }
            if (Uri.IsWellFormedUriString(requestUri, UriKind.Absolute))
            {
                return new Uri(requestUri, UriKind.Absolute);
            }
            if (Uri.IsWellFormedUriString(requestUri, UriKind.Relative))
            {
                return new Uri(new Uri(requestHost), requestUri);
            }
            throw new Exception();
        }
    }
}