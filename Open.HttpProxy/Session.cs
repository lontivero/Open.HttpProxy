using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	using BufferManager;

	public class Session
	{
		internal ClientHandler ClientHandler { get; set; }
		internal ServerHandler ServerHandler { get; set; }

		internal Pipe ClientPipe { get; set; }

		internal Pipe ServerPipe { get; set; }

		internal async Task<Pipe> EnsureConnectedToServerAsync()
		{
			if (ServerPipe == null)
			{
				var connection = await ConnectToHostAsync();
				ServerPipe = new Pipe(new ConnectionStream(connection));
				ServerHandler = new ServerHandler(this);
			}
			return ServerPipe;
		}

		internal TraceSource Trace => HttpProxy.Trace;

		public Session(Connection clientConnection, BufferAllocator bufferAllocator)
		{
			Id = Guid.NewGuid();
			ClientPipe = new Pipe(new ConnectionStream(clientConnection));
			ClientHandler = new ClientHandler(this);
			BufferAllocator = bufferAllocator;
		}

		private Session(Pipe clientPipe, Pipe serverPipe, BufferAllocator bufferAllocator)
		{
			Id = Guid.NewGuid();
			ClientPipe = clientPipe;
			ClientHandler = new ClientHandler(this);
			ServerPipe = serverPipe;
			ServerHandler = new ServerHandler(this);
			BufferAllocator = bufferAllocator;
		}

		public Guid Id { get; }

		public BufferAllocator BufferAllocator { get; }

		public string ErrorMessage { get; set; }

		public int ErrorStatus { get; set; }

		public Request Request { get; internal set; }

		public Response Response { get; internal set; }

		public bool HasResponse => Response != null;

		internal bool HasError { get; set; }

		public bool IsHttps => Request.IsHttps;

		public bool IsWebSocketHandshake =>
			(Request != null && Request.IsWebSocketHandshake) &&
			(Response != null && Response.IsWebSocketHandshake);

		public async Task<Connection> ConnectToHostAsync()
		{
			var uri = Request.Uri;
			return await ServerHandler.ConnectToHostAsync(uri);
		}

		public Session Clone()
		{
			return new Session(ClientPipe, ServerPipe, BufferAllocator);
		}
	}
}