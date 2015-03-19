using System;
using System.Net;
using System.Net.Sockets;
using Open.HttpProxy.EventArgs;
using Open.Tcp;
using Open.Tcp.Utils;

namespace Open.HttpProxy.Listeners
{
    public enum ListenerStatus
    {
        Listening,
        Stopped
    }

    internal class TcpListener
    {
        public event EventHandler<ConnectionEventArgs> ConnectionRequested;

        private static readonly BlockingPool<SocketAsyncEventArgs> ConnectSaeaPool =
            new BlockingPool<SocketAsyncEventArgs>(() => new SocketAsyncEventArgs());

        protected IPEndPoint EndPoint;
        protected Socket Listener;
        private readonly int _port;
        private ListenerStatus _status;

        public TcpListener(int port)
        {
            _port = port;
            _status = ListenerStatus.Stopped;
            EndPoint = new IPEndPoint(IPAddress.Any, port);
            ConnectSaeaPool.PreAllocate(100);
            Connection.AwaitableSocketPool.PreAllocate(200);
        }

        public ListenerStatus Status
        {
            get { return _status; }
        }

        public EndPoint Endpoint
        {
            get { return EndPoint; }
        }

        public int Port
        {
            get { return _port; }
        }

        public void Start()
        {
            try
            {
                Listener = CreateSocket();
                _status = ListenerStatus.Listening;

                Listen();
            }
            catch (SocketException)
            {
                if (Listener == null) return;
                Stop();
                throw;
            }
        }

        private Socket CreateSocket()
        {
            var socket = new Socket(EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //          socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
            socket.Bind(EndPoint);
            socket.Listen(4);
            return socket;
        }

        private void Notify(SocketAsyncEventArgs saea)
        {
            saea.AcceptSocket.NoDelay = true;
            var connection = new Connection(saea.AcceptSocket);
            Events.Raise(ConnectionRequested, this, new ConnectionEventArgs(connection));
        }

        private void Listen()
        {
            var saea = ConnectSaeaPool.Take();
            saea.AcceptSocket = null;
            saea.Completed += IOCompleted;
            if(_status == ListenerStatus.Stopped) return;

            var async = Listener.AcceptAsync(saea);

            if (!async)
            {
                IOCompleted(null, saea);
            }
        }

        private void IOCompleted(object sender, SocketAsyncEventArgs saea)
        {
            if (Listener != null) Listen();
            try
            {
                if (saea.SocketError == SocketError.Success)
                {
                    Notify(saea);
                }
            }
            finally
            {
                saea.Completed -= IOCompleted;
                ConnectSaeaPool.Add(saea);
            }
        }

        public void Stop()
        {
            _status = ListenerStatus.Stopped;
            if (Listener != null)
            {
                Listener.Close();
                Listener = null;
            }
        }
    }
}