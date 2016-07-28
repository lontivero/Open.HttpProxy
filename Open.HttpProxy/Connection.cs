//
// - Connection.cs
// 
// Author:
//	 Lucas Ontivero <lucasontivero@gmail.com>
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

namespace Open.HttpProxy
{
	using Utils;
	
	public class Connection
	{
		internal static readonly BlockingPool<SocketAwaitable> AwaitableSocketPool =
			new BlockingPool<SocketAwaitable>(() => new SocketAwaitable(new SocketAsyncEventArgs()));

		private readonly Socket _socket;
		private readonly IPEndPoint _endpoint;
		private readonly Uri _uri;
		private bool _socketDisposed;

		public IPEndPoint Endpoint => _endpoint;

		public Uri Uri => _uri;

		public bool IsConnected => _socket.Connected;

		public bool Available => _socket.Available > 0;

		internal Connection(IPEndPoint endpoint)
			: this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), endpoint)
		{}

		internal Connection(Socket socket)
			: this(socket, (IPEndPoint)socket.RemoteEndPoint)
		{}

		internal Connection(Socket socket, IPEndPoint endpoint)
		{
			_socket = socket;
			_socket.NoDelay = true;
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
				var bytesRead = awaitableSocket.EventArgs.BytesTransferred;
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

			try
			{
				await _socket.SendAsync(awaitableSocket);
				var bytesWrite = awaitableSocket.EventArgs.BytesTransferred;
				return bytesWrite;
			}
			finally
			{
				AwaitableSocketPool.Add(awaitableSocket);
			}
		}

		public async Task ConnectAsync()
		{
			var awaitableSocket = AwaitableSocketPool.Take();
			awaitableSocket.EventArgs.RemoteEndPoint = Endpoint;
			awaitableSocket.EventArgs.SetBuffer(new byte[0], 0, 0 ); // data can be sent otherwise
			await _socket.ConnectAsync(awaitableSocket);
		}

		public void Close()
		{
			if (_socket.Connected)
			{
				_socket.LingerState = new LingerOption(true, 0);
				_socket.Shutdown(SocketShutdown.Both);
				_socket.Close();
			}
			_socketDisposed = true;
		}
	}
}