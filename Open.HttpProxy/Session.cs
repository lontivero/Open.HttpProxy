using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
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
		public IPEndPoint Endpoint { get; set; }
		public HttpProxy Proxy { get; set; }

		internal ClientHandler ClientHandler { get; set; }
		internal ServerHandler ServerHandler { get; set; }

		internal Pipe ClientPipe { get; set; }

		internal Pipe ServerPipe { get; set; }

		internal SessionFlag Flags { get; }

		internal State CurrentState { get; set; }

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

		internal Logger Logger => HttpProxy.Logger;

		public Session(Stream clientConnection, IPEndPoint endpoint, HttpProxy proxy)
		{
			Endpoint = endpoint;
			Proxy = proxy;
			Id = Guid.NewGuid();
			ClientPipe = new Pipe(clientConnection);
			ClientHandler = new ClientHandler(this);
			Flags = new SessionFlag();
			CurrentState = State.ReceivingHeaders;
		}

		internal Session(Pipe clientPipe, Pipe serverPipe, HttpProxy proxy)
		{
			Id = Guid.NewGuid();
			ClientPipe = clientPipe;
			ClientHandler = new ClientHandler(this);
			ServerPipe = serverPipe;
			ServerHandler = new ServerHandler(this);
			Flags = new SessionFlag();
			CurrentState = State.ReceivingHeaders;
			Proxy = proxy;
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

		internal Session Clone()
		{
			return new Session(ClientPipe, ServerPipe, Proxy);
		}

		internal void RaiseRequestHeadersReceivedEvent()
		{
			Events.Raise(Proxy?.OnRequestHeaders, this, new SessionEventArgs(this));
		}

		internal void RaiseRequestReceivedEvent()
		{
			Events.Raise(Proxy?.OnRequest, this, new SessionEventArgs(this));
		}

		internal void RaiseResponseHeadersReceivedEvent()
		{
			Events.Raise(Proxy?.OnResponseHeaders, this, new SessionEventArgs(this));
		}

		internal void RaiseResponseReceivedEvent()
		{
			Events.Raise(Proxy?.OnResponse, this, new SessionEventArgs(this));
		}

		public void Abort()
		{
			CurrentState = State.Aborted;
		}

		public async Task ReturnResponseAsync(Response response)
		{
			if (CurrentState == State.ReceivingHeaders)
			{
				await ClientHandler.ReceiveBodyAsync().WithoutCapturingContext();
			}
			await ClientHandler.ReturnResponseAsync(response, true);
			CurrentState = State.Aborted;
		}
	}
}