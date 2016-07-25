using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	using BufferManager;

	public class Session
	{
		private readonly Connection _clientConnection;
		private readonly ClientHandler _clientHandler;
		private Connection _serverConnection;
		private ServerHandler _serverHandler;

		public Session(Connection clientConnection, Connection serverConnection,  BufferAllocator bufferAllocator)
		{
			_clientConnection = clientConnection;
			_serverConnection = serverConnection;

			BufferAllocator = bufferAllocator;
			Request = new Request();
			Response = new Response();
			_clientHandler = new ClientHandler(this, _clientConnection);
			if(_serverConnection!=null)
				_serverHandler = new ServerHandler(this, _serverConnection);
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

		public async Task<Connection> ConnectToHostAsync()
		{
			if (_serverConnection == null)
			{
				var uri = Request.GetUriFromRequest();
				_serverConnection = await ServerHandler.ConnectToHostAsync(uri);
				_serverHandler = new ServerHandler(this, _serverConnection);
			}
			return _serverConnection;
		}
	}
}