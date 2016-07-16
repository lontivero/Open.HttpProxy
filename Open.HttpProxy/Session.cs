using System.IO;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	using BufferManager;

	internal class Session
	{
		private readonly Connection _clientConnection;
	    private readonly ServerHandler _serverHandler;

		public Session(Connection clientConnection, BufferAllocator bufferAllocator)
		{
			_clientConnection = clientConnection;
			BufferAllocator = bufferAllocator;
			Request = new Request(this);
			Response = new Response(this);
			ClientHandler = new ClientHandler(this, _clientConnection);
			_serverHandler = new ServerHandler(this);
		}

		public BufferAllocator BufferAllocator { get; }

	    public async Task ReceiveRequestAsync()
		{
			await ClientHandler.ReceiveEntityAsync();
	        await ClientHandler.ReceiveBodyAsync();
		}

		public string ErrorMessage { get; set; }
		public int ErrorStatus { get; set; }

		public Request Request { get; }

	    public Response Response { get; }

	    internal bool HaveError { get; set; }

		internal ClientHandler ClientHandler { get; }

	    internal async Task ReturnResponse()
		{
			var stream = new BufferedStream(new ConnectionStream(_clientConnection));
			var writer = new StreamWriter(stream);
			await writer.WriteAsync(Response.StatusLine.ResponseLine.ToCharArray());
			await writer.WriteAsync("\r\n".ToCharArray());
			await writer.WriteAsync(Response.Headers.ToCharArray());
			await writer.WriteAsync(Response.Body.ToCharArray());
			await writer.FlushAsync();
			stream.Close();
		}

		public async Task ResendRequestAsync()
		{
			await _serverHandler.ConnectToHostAsync();
		    await _serverHandler.SendEntityAsync();
            await _serverHandler.SendBodyAsync();
        }
    }
}