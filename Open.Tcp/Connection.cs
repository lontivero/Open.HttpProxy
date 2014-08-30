//
// - Connection.cs
// 
// Author:
//     Lucas Ontivero <lucasontivero@gmail.com>
// 
// Copyright 2013 Lucas E. Ontivero
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//  http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 

// <summary></summary>

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Open.Tcp.Utils;


namespace Open.Tcp
{
    public class Connection
    {
        private static readonly BlockingPool<SocketAwaitable> AwaitableSocketPool =
            new BlockingPool<SocketAwaitable>(() => new SocketAwaitable(new SocketAsyncEventArgs()));

        private readonly Socket _socket;
        private readonly IPEndPoint _endpoint;
        private readonly Uri _uri;
        private bool _socketDisposed;

        public IPEndPoint Endpoint
        {
            get { return _endpoint; }
        }

        public Uri Uri
        {
            get { return _uri; }
        }

        public bool IsConnected
        {
            get { return _socket.Connected; }
        }

        internal Connection(IPEndPoint endpoint)
            : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), endpoint)
        {}

        internal Connection(Socket socket)
            : this(socket, (IPEndPoint)socket.RemoteEndPoint)
        {}

        internal Connection(Socket socket, IPEndPoint endpoint)
        {
            _socket = socket;
            _endpoint = endpoint;
            _socketDisposed = false;
            _uri = new Uri("tcp://" + _endpoint.Address + ':' + _endpoint.Port);
        }


        public async Task<int> ReceiveAsync(byte[] array, int offset, int count)
        {
            var awaitableSocket = AwaitableSocketPool.Take();
            awaitableSocket.EventArgs.SetBuffer(array, offset, count);

            try
            {
                await _socket.ReceiveAsync(awaitableSocket);
                int bytesRead = awaitableSocket.EventArgs.BytesTransferred;
                return bytesRead;
            }
            finally
            {
                AwaitableSocketPool.Add(awaitableSocket);
            }
        }

        public async Task<int> SendAsync(byte[] array, int offset, int count)
        {
            var awaitableSocket = AwaitableSocketPool.Take();
            awaitableSocket.EventArgs.SetBuffer(array, offset, count);
            var totalSent = 0;

            try
            {
                while (true)
                {
                    await _socket.ReceiveAsync(awaitableSocket);
                    int bytesWrite = awaitableSocket.EventArgs.BytesTransferred;
                    if (bytesWrite <= 0) break;
                    totalSent += bytesWrite;
                }

                return totalSent;
            }
            finally
            {
                AwaitableSocketPool.Add(awaitableSocket);
            }
        }

        public async Task ConnectAsync()
        {
            //if (_socketDisposed){ callback(false); return;}

            var awaitableSocket = AwaitableSocketPool.Take();
            awaitableSocket.EventArgs.RemoteEndPoint = Endpoint;

            await _socket.ConnectAsync(awaitableSocket);
        }

        public void Close()
        {
            _socket.Close();
            _socketDisposed = true;
        }
    }
}