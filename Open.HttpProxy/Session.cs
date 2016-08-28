using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Open.HttpProxy.Utils;

namespace Open.HttpProxy
{
	class SessionFlag
	{
		private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>();

		public bool this[string name]
		{
			get { return _flags.ContainsKey(name) && _flags[name]; }
			set { _flags[name] = value; }
		}
	}

	public class Session
	{
		internal ClientHandler ClientHandler { get; set; }
		internal ServerHandler ServerHandler { get; set; }

		internal Pipe ClientPipe { get; set; }

		internal Pipe ServerPipe { get; set; }

		internal SessionFlag Flags { get; }

		internal async Task<Pipe> EnsureConnectedToServerAsync(Uri uri)
		{
			if (ServerPipe == null)
			{
				var socket = await ConnectToHostAsync(uri).WithoutCapturingContext();
				ServerPipe = new Pipe(new NetworkStream(socket));
				ServerHandler = new ServerHandler(this);
			}
			return ServerPipe;
		}

		internal TraceSource Trace => HttpProxy.Trace;

		public Session(Stream clientConnection)
		{
			Id = Guid.NewGuid();
			ClientPipe = new Pipe(clientConnection);
			ClientHandler = new ClientHandler(this);
			Flags = new SessionFlag();
		}

		private Session(Pipe clientPipe, Pipe serverPipe)
		{
			Id = Guid.NewGuid();
			ClientPipe = clientPipe;
			ClientHandler = new ClientHandler(this);
			ServerPipe = serverPipe;
			ServerHandler = new ServerHandler(this);
			Flags = new SessionFlag();
		}

		public Guid Id { get; }

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

		public async Task<Socket> ConnectToHostAsync(Uri uri)
		{
			return await ServerHandler.ConnectToHostAsync(uri).WithoutCapturingContext();
		}

		public Session Clone()
		{
			return new Session(ClientPipe, ServerPipe);
		}
	}
}