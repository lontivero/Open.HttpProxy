using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Open.HttpProxy.Utils;

namespace Open.HttpProxy
{

	static class ProcessActions
	{
		public static async Task<Enum> ReceiveRequestHeadersAsync(Session ctx)
		{
			var handler = ctx.ClientHandler;
			await handler.ReceiveAsync().WithoutCapturingContext();
			var request = ctx.Request;
			if (request?.RequestLine == null)
			{
				ctx.Trace.TraceEvent(TraceEventType.Warning, 0, "No request received. We are done with this.");
				return Command.Error;
			}

			if (request.Uri.IsLoopback && request.Uri.Port == ctx.Endpoint.Port)
			{
				await ctx.ClientHandler.SendErrorAsync(request.RequestLine.Version, 200, "Open.HttpProxy working", "This is a proxy server man....").WithoutCapturingContext();
				return Command.Error;
			}

			if (request.RequestLine.IsVerb("CONNECT"))
			{
				return request.Uri.Port != 80
					? Command.AuthenticateClient
					: Command.CreateTunnel;
			}
			return Command.ReceiveBody;
		}

		public static async Task<Enum> ReceiveRequestBodyAsync(Session ctx)
		{
			await ctx.ClientHandler.ReceiveBodyAsync().WithoutCapturingContext();
			return Command.SendRequestHeaders;
		}


		public static async Task<Enum> SendRequestAsync(Session ctx)
		{
			await ctx.EnsureConnectedToServerAsync(ctx.Request.Uri).WithoutCapturingContext();
			var handler = ctx.ServerHandler;
			await handler.ResendRequestAsync().WithoutCapturingContext();
			return Command.ReceiveResponse;
		}

		public static async Task<Enum> ReceiveResponseAsync(Session ctx)
		{
			var handler = ctx.ServerHandler;
			await handler.ReceiveResponseAsync().WithoutCapturingContext();
			var response = ctx.Response;
			if (response?.StatusLine == null)
			{
				ctx.Trace.TraceEvent(TraceEventType.Warning, 0, "No response received. We are done with this.");
				return Command.Error;
			}

			if (!response.KeepAlive)
			{
				ctx.Trace.TraceInformation("No keep-alive from server response. Closing.");
				handler.Close();
			}
			return Command.SendResponse;
		}


		public static async Task<Enum> SendResponseAsync(Session ctx)
		{
			var handler = ctx.ClientHandler;
			await handler.ResendResponseAsync().WithoutCapturingContext();

			//if (ctx.IsWebSocketHandshake)
			if (ctx.Request.Headers.Upgrade != null)
			{
				ctx.Trace.TraceEvent(TraceEventType.Verbose, 0, "WebSocket handshake");
				return Command.UpgradeToWebSocketTunnel;
			}

			if (!ctx.Request.KeepAlive && !ctx.Response.KeepAlive)
			{
				ctx.Trace.TraceEvent(TraceEventType.Verbose, 0, "No keep-alive... closing handler");
				handler.Close();
				return Command.Exit;
			}

			ctx.Trace.TraceEvent(TraceEventType.Verbose, 0, "keep-alive!");
			var p = StateMachineBuilder.Build();
			await p.RunAsync(ctx.Clone()).WithoutCapturingContext();
			return Command.Exit;
		}

		public static async Task<Enum> AuthenticateClientAsync(Session ctx)
		{
			var uri = ctx.Request.Uri;
			await ctx.ClientHandler.CreateHttpsTunnelAsync().WithoutCapturingContext();
			await ctx.EnsureConnectedToServerAsync(uri).WithoutCapturingContext();
			await ctx.ServerHandler.CreateHttpsTunnelAsync().WithoutCapturingContext();
			ctx.Response = null;
			ctx.Request = null;
			return Command.ReceiveRequestHeaders;
		}

		public static async Task<Enum> AuthenticateServerAsync(Session ctx)
		{
			await ctx.EnsureConnectedToServerAsync(ctx.Request.Uri).WithoutCapturingContext();
			await ctx.ServerHandler.CreateHttpsTunnelAsync().WithoutCapturingContext();
			ctx.Response = null;
			return Command.SendRequestHeaders;
		}

		public static async Task<Enum> CreateTunnelAsync(Session ctx)
		{
			var clientStream = ctx.ClientPipe.Stream;
			var requestLine = ctx.Request.RequestLine;

			await ctx.ClientHandler.BuildAndReturnResponseAsync(requestLine.Version, 200, "Connection established").WithoutCapturingContext();
			await ctx.EnsureConnectedToServerAsync(ctx.Request.Uri).WithoutCapturingContext();
			var serverStream = ctx.ServerPipe.Stream;

			var sendTask = clientStream.CopyToAsync(serverStream);
			var receiveTask = serverStream.CopyToAsync(clientStream);

			await Task.WhenAll(sendTask, receiveTask).WithoutCapturingContext();
			return Command.Exit;
		}

		public static async Task<Enum> SetWebSocketTunnelAsync(Session ctx)
		{
			var clientStream = ctx.ClientPipe.Stream;
			await ctx.EnsureConnectedToServerAsync(ctx.Request.Uri).WithoutCapturingContext();
			var serverStream = ctx.ServerPipe.Stream;

			var sendTask = clientStream.CopyToAsync(serverStream);
			var receiveTask = serverStream.CopyToAsync(clientStream);

			await Task.WhenAll(sendTask, receiveTask).WithoutCapturingContext();
			return Command.Exit;
		}

		public static async Task<Enum> FinishAsync(Session ctx)
		{
			try
			{
				ctx.ClientHandler?.Close();
				ctx.ServerHandler?.Close();
			}
			catch (Exception e)
			{
				/* nothing to do*/
			}
			return await Task.FromResult(Command.Continue).WithoutCapturingContext();
		}
	}

	enum State
	{
		Initial,
		ReceivingBody,
		Done,
		AuthenticatingClient,
		CreatingTunnel,
		SendingRequestHeaders,
		AuthenticatingClientServer,
		AuthenticatingServer,
		ReceivingHeaders,
		ReceivingResponse,
		SendingResponse,
		UpgradingToWebSocketTunnel
	}
	enum Command
	{
		Start,
		ReceiveRequestHeaders,
		AuthenticateClient,
		Continue,
		ReceiveBody,
		CreateTunnel,
		SendRequestHeaders,
		AuthenticateServer,
		Error,
		ReceiveResponse,
		SendResponse,
		UpgradeToWebSocketTunnel,
		Exit
	}


	static class StateMachineBuilder
	{
		static readonly Dictionary<Enum, Func<Session, Task<Enum>>> ActionTable = new Dictionary<Enum, Func<Session, Task<Enum>>>()
			{
				{State.ReceivingHeaders, ProcessActions.ReceiveRequestHeadersAsync},
				{State.ReceivingBody, ProcessActions.ReceiveRequestBodyAsync},
				{State.AuthenticatingClient, ProcessActions.AuthenticateClientAsync},
				{State.CreatingTunnel, ProcessActions.CreateTunnelAsync},
				{State.SendingRequestHeaders, ProcessActions.SendRequestAsync},
				{State.AuthenticatingServer, ProcessActions.AuthenticateServerAsync},
				{State.ReceivingResponse, ProcessActions.ReceiveResponseAsync },
				{State.SendingResponse, ProcessActions.SendResponseAsync },
				{State.UpgradingToWebSocketTunnel, ProcessActions.SetWebSocketTunnelAsync },
				{State.Done, ProcessActions.FinishAsync},
			};

		public static StateMachine Build()
		{
			var sm = new StateMachine(State.Initial, State.Done, Command.Start, ActionTable, null);
			sm.OnState(State.Initial)
				.If(Command.Start)
				.Then(State.ReceivingHeaders, "Receiving Request line and headers");

			sm.OnState(State.ReceivingHeaders)
				.If(Command.AuthenticateClient).Then(State.AuthenticatingClient, "Authenticating as a client")
				.If(Command.ReceiveBody).Then(State.ReceivingBody, "Receiving body")
				.If(Command.CreateTunnel).Then(State.CreatingTunnel, "Creating tunnel")
				.If(Command.Error).Then(State.Done, "There was an error and we are donde. Closing connections");

			sm.OnState(State.AuthenticatingClient)
				.If(Command.ReceiveRequestHeaders).Then(State.ReceivingHeaders, "Receiving Request line and headers over SSL");

			sm.OnState(State.ReceivingBody)
				.If(Command.SendRequestHeaders).Then(State.SendingRequestHeaders, "Sending request headers to server")
				.If(Command.AuthenticateServer).Then(State.AuthenticatingServer, "Authenticating as a server");

			sm.OnState(State.AuthenticatingServer)
				.If(Command.SendRequestHeaders).Then(State.SendingRequestHeaders, "Sending request over SSL");

			sm.OnState(State.SendingRequestHeaders)
				.If(Command.ReceiveResponse).Then(State.ReceivingResponse, "Receiving response");

			sm.OnState(State.ReceivingResponse)
				.If(Command.SendResponse).Then(State.SendingResponse, "Sending response back to client");

			sm.OnState(State.SendingResponse)
				.If(Command.UpgradeToWebSocketTunnel).Then(State.UpgradingToWebSocketTunnel, "Upgrading connection to web socket")
				.If(Command.Exit).Then(State.Done, "Closing connections");

			return sm;
		}
	}
}
