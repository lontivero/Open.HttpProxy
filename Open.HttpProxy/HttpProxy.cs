
using System;
using System.Net.Sockets;
using Open.HttpProxy.Utils;

namespace Open.HttpProxy
{
	using BufferManager;
	using EventArgs;
	using Listeners;
	
	public class HttpProxy
	{
		private readonly TcpListener _listener;
		private readonly BufferAllocator _bufferAllocator;
		public EventHandler<SessionEventArgs> OnRequest;
		public EventHandler<SessionEventArgs> OnResponse;

		public HttpProxy(int port=8888)
		{
			_listener = new TcpListener(port);
			_bufferAllocator = new BufferAllocator(new byte[1024*1024]);
			_listener.ConnectionRequested += OnConnectionRequested;
		}

		public void Start()
		{
			_listener.Start();
		}

		public void Stop()
		{
			_listener.Stop();	
		}

		private async void OnConnectionRequested(object sender, ConnectionEventArgs e)
		{
			var keepAlive = true;
			while (keepAlive)
			{
				var session = new Session(e.Connection, _bufferAllocator);
				try
				{
					await session.ReceiveRequestAsync();
					//keepAlive = session.Request.KeepAlive;
					Events.Raise(OnRequest, this, new SessionEventArgs(session));
					if (!session.HasError)
					{
						await session.ResendRequestAsync();
						await session.ReceiveResponseAsync();
						Events.Raise(OnResponse, this, new SessionEventArgs(session));
					}
					await session.ResendResponseAsync();
				}
				catch (SocketException se)
				{
					e.Connection.Close();
					return;
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					try
					{
						await session.ClientHandler.BuildAndReturnResponseAsync("HTTP/1.1", 502, $"Bad Gateway - {ex.Message}");
					}
					catch (Exception)
					{
					}
				}
			}
			e.Connection.Close();
		}
	}
}
