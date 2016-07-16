
using System;

namespace Open.HttpProxy
{
	using BufferManager;
	using EventArgs;
	using Listeners;
	
	public class HttpProxy
	{
		private readonly TcpListener _listener;
		private readonly BufferAllocator _bufferAllocator;

	    public HttpProxy(int port=8888)
		{
		    _listener = new TcpListener(port);
			_bufferAllocator = new BufferAllocator(new byte[1024*1024]);
			_listener.ConnectionRequested += OnConnectionRequested;
		}

		private async void OnConnectionRequested(object sender, ConnectionEventArgs e)
		{
            var session = new Session(e.Connection, _bufferAllocator);
		    try
		    {
		        await session.ReceiveRequestAsync();
		        await session.ResendRequestAsync();
		    }
		    catch (Exception)
		    {
		        await session.ClientHandler.BuildAndReturnResponseAsync(502, "Bad Gateway");
		    }
		}

		public void Start()
		{
			_listener.Start();
		}
	}
}
