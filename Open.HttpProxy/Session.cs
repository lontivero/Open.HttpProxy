using System.Threading.Tasks;

namespace Open.HttpProxy
{
	using BufferManager;

	public class Session
	{
		private readonly Connection _clientConnection;
		private readonly ClientHandler _clientHandler;
		private readonly ServerHandler _serverHandler;

		public Session(Connection clientConnection, BufferAllocator bufferAllocator)
		{
			_clientConnection = clientConnection;
			BufferAllocator = bufferAllocator;
			Request = new Request();
			Response = new Response();
			_clientHandler = new ClientHandler(this, _clientConnection);
			_serverHandler = new ServerHandler(this);
		}

		internal ClientHandler ClientHandler => _clientHandler;

		public BufferAllocator BufferAllocator { get; }

		public string ErrorMessage { get; set; }
		public int ErrorStatus { get; set; }

		public Request Request { get; internal set; }

		public Response Response { get; internal set; }

		internal bool HasError { get; set; }

		public bool IsHttps => Request.IsHttps;

		public async Task ReceiveRequestAsync()
		{
			await _clientHandler.ReceiveAsync();
			await _clientHandler.ReceiveBodyAsync();
		}

		public async Task ResendRequestAsync()
		{
			await _serverHandler.ConnectToHostAsync();
			await _serverHandler.SendEntityAsync();
			await _serverHandler.SendBodyAsync();
		}

		public async Task ReceiveResponseAsync()
		{
			await _serverHandler.ReceiveEntityAsync();
			await _serverHandler.ReceiveBodyAsync();
		}

		public async Task ResendResponseAsync()
		{
			await _clientHandler.SendEntityAsync();
			await _clientHandler.SendBodyAsync();
		}
	}
}