
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
			_bufferAllocator = new BufferAllocator(new byte[20*1024*1024]);
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
			Connection clientConnection = e.Connection;
			Connection serverConnection = null;

			var keepAlive = true;
			while (keepAlive)
			{
				var session = new Session(clientConnection, serverConnection, _bufferAllocator);
				try
				{
					await session.ReceiveRequestAsync();
					Events.Raise(OnRequest, this, new SessionEventArgs(session));
					if (!session.HasError)
					{
						serverConnection = await session.ConnectToHostAsync();
						await session.ResendRequestAsync();
						await session.ReceiveResponseAsync();
						Events.Raise(OnResponse, this, new SessionEventArgs(session));
						keepAlive = session.Response.KeepAlive;
					}
					await session.ResendResponseAsync();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					try
					{
						var ver = ProtocolVersion.Parse("HTTP/1.1");
						await session.ClientHandler.BuildAndReturnResponseAsync(ver, 502, $"Bad Gateway - {ex.Message}");
					}
					catch (Exception)
					{
					}
					break;
				}
			}
			clientConnection?.Close();
			serverConnection?.Close();
		}
	}
}
