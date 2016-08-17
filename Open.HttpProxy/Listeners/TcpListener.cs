using System;
using System.Net;
using System.Net.Sockets;

namespace Open.HttpProxy.Listeners
{
	using EventArgs;
	using Utils;
	
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

		private readonly IPEndPoint _endPoint;
		private Socket _listener;
		private ListenerStatus _status;
		public int Port { get; }

		public TcpListener(int port)
		{
			Port = port;
			_status = ListenerStatus.Stopped;
			_endPoint = new IPEndPoint(IPAddress.Any, port);
			ConnectSaeaPool.PreAllocate(100);
			Connection.AwaitableSocketPool.PreAllocate(200);
		}

		public ListenerStatus Status => _status;

		public EndPoint Endpoint => _endPoint;

		public void Start()
		{
			if (Status == ListenerStatus.Listening)
			{
				throw new InvalidOperationException("Already listing");
			}
			try
			{
				_listener = CreateSocket();
				_status = ListenerStatus.Listening;

				Listen();
			}
			catch (SocketException)
			{
				if (_listener == null) return;
				Stop();
				throw;
			}
		}

		private Socket CreateSocket()
		{
			var socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			//		  socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
			socket.Bind(_endPoint);
			socket.Listen(2147483647);
			return socket;
		}

		private void Notify(SocketAsyncEventArgs saea)
		{
			var connection = new Connection(saea.AcceptSocket);
			Events.Raise(ConnectionRequested, this, new ConnectionEventArgs(connection));
		}

		private void Listen()
		{
			var saea = ConnectSaeaPool.Take();
			saea.AcceptSocket = null;
			saea.Completed += IoCompleted;
			if(_status == ListenerStatus.Stopped) return;

			var async = _listener.AcceptAsync(saea);

			if (!async)
			{
				IoCompleted(null, saea);
			}
		}

		private void IoCompleted(object sender, SocketAsyncEventArgs saea)
		{
			if (_listener != null) Listen();
			try
			{
				if (saea.SocketError == SocketError.Success)
				{
					Notify(saea);
				}
				else
				{
					// bad connect. Close socket because it could be in a unknown state
					saea.AcceptSocket.Close();
				}
			}
			finally
			{
				saea.Completed -= IoCompleted;
				ConnectSaeaPool.Add(saea);
			}
		}

		public void Stop()
		{
			_status = ListenerStatus.Stopped;
			if (_listener != null)
			{
				_listener.Close();
				_listener = null;
			}
		}
	}
}