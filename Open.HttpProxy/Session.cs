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


	// ======================================================================
	interface IProcessingState
	{
		Task ProcessAsync(ProcessingContext ctx);
	}

	class ReceiveRequestState : IProcessingState
	{
		public async Task ProcessAsync(ProcessingContext ctx)
		{
			using (new TraceScope(ctx.Session.Trace, "Receiving request"))
			{
				var handler = ctx.Session.ClientHandler;
				await handler.ReceiveAsync();
				var request = ctx.Session.Request;
				if (request?.RequestLine == null)
				{
					ctx.Session.Trace.TraceEvent(TraceEventType.Warning, 0, "No request received. We are done with this.");
					ctx.NextState(ProcessingStates.Done);
					return;
				}
				await handler.ReceiveBodyAsync();

				if (request.RequestLine.IsVerb("CONNECT"))
				{
					ctx.NextState(request.Uri.Port != 80 
						? ProcessingStates.CreateHttpsTunnel 
						: ProcessingStates.CreateTunnel);
				}
				else
				{
					ctx.NextState(ProcessingStates.SendRequest);
				}
			}
		}
	}

	class SendRequestState : IProcessingState
	{
		public async Task ProcessAsync(ProcessingContext ctx)
		{
			using (new TraceScope(ctx.Session.Trace, "Sending request to server"))
			{
				await ctx.Session.EnsureConnectedToServerAsync();
				var handler = ctx.Session.ServerHandler;
				await handler.ResendRequestAsync();
				ctx.NextState(ProcessingStates.ReceiveResponse);
			}
		}
	}

	class ReceiveResponseState : IProcessingState
	{
		public async Task ProcessAsync(ProcessingContext ctx)
		{
			using (new TraceScope(ctx.Session.Trace, "Receiving response from server"))
			{
				var handler = ctx.Session.ServerHandler;
				await handler.ReceiveResponseAsync();
				var response = ctx.Session.Response;
				if (response?.StatusLine == null)
				{
					ctx.Session.Trace.TraceEvent(TraceEventType.Warning, 0, "No response received. We are done with this.");
					ctx.NextState(ProcessingStates.Done);
					return;
				}

				if (!response.KeepAlive)
				{
					ctx.Session.Trace.TraceInformation("No keep-alive from server response. Closing.");
					handler.Close();
				}
				ctx.NextState(ProcessingStates.SendResponse);
			}
		}
	}


	class SendResponseState : IProcessingState
	{
		public async Task ProcessAsync(ProcessingContext ctx)
		{
			using (new TraceScope(ctx.Session.Trace, "Sending response back to client"))
			{
				var handler = ctx.Session.ClientHandler;
				await handler.ResendResponseAsync();

				//if (ctx.Session.IsWebSocketHandshake)
				if(ctx.Session.Request.Headers.Upgrade != null)
				{
					ctx.Session.Trace.TraceEvent(TraceEventType.Verbose, 0, "WebSocket handshake");
					ctx.NextState(ProcessingStates.UpgradeToWebSocketTunnel);
					return;
				}

				if (!ctx.Session.Request.KeepAlive && !ctx.Session.Response.KeepAlive)
				{
					ctx.Session.Trace.TraceEvent(TraceEventType.Verbose, 0, "No keep-alive... closing handler");
					handler.Close();
					ctx.NextState(ProcessingStates.Done);
				}
				else
				{
					ctx.Session.Trace.TraceEvent(TraceEventType.Verbose, 0, "keep-alive!");
					var p = new Processing(ctx.Session.Clone());
					await p.ProcessAsync();
					ctx.NextState(ProcessingStates.Done);
				}
			}
		}
	}

	class CreateHttpsTunnelState : IProcessingState
	{
		public async Task ProcessAsync(ProcessingContext ctx)
		{
			var s = ctx.Session;
			using (new TraceScope(s.Trace, "Creating HTTPS Tunnel"))
			{
				var clientTunnelTask = s.ClientHandler.CreateHttpsTunnelAsync();
				var serverTunnelTask = Task.Run(async () => {
					await s.EnsureConnectedToServerAsync();
					await s.ServerHandler.CreateHttpsTunnelAsync();
				});
				
				await Task.WhenAll(clientTunnelTask, serverTunnelTask);
				s.Request = null;
				s.Response = null;
				ctx.NextState(ProcessingStates.ReceiveRequest);
			}
		}
	}

	class CreateTunnelState : IProcessingState
	{
		public async Task ProcessAsync(ProcessingContext ctx)
		{
			using (new TraceScope(ctx.Session.Trace, "Creating WebSocket Tunnel"))
			{
				var s = ctx.Session;
				var clientStream = s.ClientPipe.Stream;
				var requestLine = s.Request.RequestLine;

				var responseTask = s.ClientHandler.BuildAndReturnResponseAsync(requestLine.Version, 200, "Connection established");
				var connectServerTask = Task.Run(async () => {
					await ctx.Session.EnsureConnectedToServerAsync();
					var serverStream = ctx.Session.ServerPipe.Stream;

					var sendTask = clientStream.CopyToAsync(serverStream);
					var receiveTask = serverStream.CopyToAsync(clientStream);

					await Task.WhenAll(sendTask, receiveTask);
				});
				await Task.WhenAll(responseTask, connectServerTask);
				ctx.IsFinished = true;
			}
		}
	}

	class SetWebSocketTunnel : IProcessingState
	{
		public async Task ProcessAsync(ProcessingContext ctx)
		{
			using (new TraceScope(ctx.Session.Trace, "Creating WebSocket Tunnel"))
			{
				var clientStream = ctx.Session.ClientPipe.Stream;
				await ctx.Session.EnsureConnectedToServerAsync();
				var serverStream = ctx.Session.ServerPipe.Stream;

				var sendTask = clientStream.CopyToAsync(serverStream);
				var receiveTask = serverStream.CopyToAsync(clientStream);

				await Task.WhenAll(sendTask, receiveTask);
				ctx.IsFinished = true;
			}
		}
	}

	class DoneState : IProcessingState
	{
		public Task ProcessAsync(ProcessingContext ctx)
		{
			try
			{
				var s = ctx.Session;
				s.ClientHandler?.Close();
				s.ServerHandler?.Close();
			}
			catch (Exception e)
			{
				 /* nothing to do*/
			}
			finally
			{
				ctx.IsFinished = true;
			}
			return Task.CompletedTask;
		}
	}


	internal class Processing
	{
		private readonly Session _session;
		private readonly ProcessingContext _context;

		public Processing(Session session)
		{
			_session = session;
			_context = new ProcessingContext(session);
		}

		public async Task ProcessAsync()
		{
			using (new TraceScope(HttpProxy.Trace, $"Procession session {_session.Id}"))
			{
				while (!_context.IsFinished)
				{
					await _context.ProcessAsync();
				}
			}
		}
	}


	internal class ProcessingContext
	{
		private IProcessingState _state;
		internal Session Session { get; set; }
		public bool IsFinished { get; set; }

		public ProcessingContext(Session session)
		{
			Session = session;
			_state = ProcessingStates.ReceiveRequest;
		}

		public void NextState(IProcessingState state)
		{
			_state = state;
		}

		public async Task ProcessAsync()
		{
			await _state.ProcessAsync(this);
		}
	}

	static class ProcessingStates
	{
		public static IProcessingState ReceiveRequest = new ReceiveRequestState();
		public static IProcessingState SendRequest = new SendRequestState();
		public static IProcessingState ReceiveResponse = new ReceiveResponseState();
		public static IProcessingState SendResponse = new SendResponseState();
		public static IProcessingState CreateHttpsTunnel = new CreateHttpsTunnelState();
		public static IProcessingState CreateTunnel = new CreateTunnelState();
		public static IProcessingState Done = new DoneState();
		public static IProcessingState UpgradeToWebSocketTunnel = new SetWebSocketTunnel();
	}
}