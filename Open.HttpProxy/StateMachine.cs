using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Open.HttpProxy.Utils;

namespace Open.HttpProxy
{
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

	public class StateMachine
	{
		public static async Task RunAsync(Session session)
		{
			using (session.Logger.Enter($"Procession session {session.Id}"))
			{
				do
				{
					session.CurrentState = await DoAsync(session).WithoutCapturingContext();
				} while (session.CurrentState != State.Done);
			}
		}

		private static async Task<State> DoAsync(Session ctx)
		{
			var state = ctx.CurrentState;

			switch (state)
			{
				case State.Initial:
					break;

				case State.ReceivingHeaders:
					ctx.Logger.Info("Receiving Request line and headers");
					var handler = ctx.ClientHandler;
					await handler.ReceiveAsync().WithoutCapturingContext();
					var request = ctx.Request;
					if (request?.RequestLine == null)
					{
						ctx.Logger.Warning("No request received. We are done with this.");
						return State.Done;
					}

					if (request.Uri.IsLoopback && request.Uri.Port == ctx.Endpoint.Port)
					{
						await ctx.ClientHandler.SendErrorAsync(request.RequestLine.Version, 200, "Open.HttpProxy working", "This is a proxy server man....").WithoutCapturingContext();
						return State.Done;
					}

					if (request.RequestLine.IsVerb("CONNECT"))
					{
						return request.Uri.Port != 80
							? State.AuthenticatingClient
							: State.CreatingTunnel;
					}
					return State.ReceivingBody;

				case State.ReceivingBody:
					ctx.Logger.Info("Receiving Request body");
					await ctx.ClientHandler.ReceiveBodyAsync().WithoutCapturingContext();
					return State.SendingRequestHeaders;

				case State.AuthenticatingClient:
					ctx.Logger.Info("Authenticating as a client");
					var uri = ctx.Request.Uri;
					await ctx.ClientHandler.CreateHttpsTunnelAsync().WithoutCapturingContext();
					await ctx.EnsureConnectedToServerAsync(uri).WithoutCapturingContext();
					await ctx.ServerHandler.CreateHttpsTunnelAsync().WithoutCapturingContext();
					ctx.Response = null;
					ctx.Request = null;
					return State.ReceivingHeaders;

				case State.CreatingTunnel:
					ctx.Logger.Info("Creating tunnel");
					var clientStream = ctx.ClientPipe.Stream;
					var requestLine = ctx.Request.RequestLine;

					await ctx.ClientHandler.BuildAndReturnResponseAsync(requestLine.Version, 200, "Connection established").WithoutCapturingContext();
					await ctx.EnsureConnectedToServerAsync(ctx.Request.Uri).WithoutCapturingContext();
					var serverStream = ctx.ServerPipe.Stream;

					var sendTask = clientStream.CopyToAsync(serverStream);
					var receiveTask = serverStream.CopyToAsync(clientStream);

					await Task.WhenAll(sendTask, receiveTask).WithoutCapturingContext();
					return State.Done;

				case State.SendingRequestHeaders:
					ctx.Logger.Info("Sending request headers to server");
					await ctx.EnsureConnectedToServerAsync(ctx.Request.Uri).WithoutCapturingContext();
					await ctx.ServerHandler.ResendRequestAsync().WithoutCapturingContext();
					return State.ReceivingResponse;

				case State.AuthenticatingServer:
					ctx.Logger.Info("Authenticating as a server");
					await ctx.EnsureConnectedToServerAsync(ctx.Request.Uri).WithoutCapturingContext();
					await ctx.ServerHandler.CreateHttpsTunnelAsync().WithoutCapturingContext();
					ctx.Response = null;
					return State.SendingRequestHeaders;

				case State.ReceivingResponse:
					ctx.Logger.Info("Receiving response from server");
					await ctx.ServerHandler.ReceiveResponseAsync().WithoutCapturingContext();
					var response = ctx.Response;
					if (response?.StatusLine == null)
					{
						ctx.Logger.Warning("No response received. We are done with this.");
						return State.Done;
					}

					if (!response.KeepAlive)
					{
						ctx.Logger.Info("No keep-alive from server response. Closing.");
						ctx.ServerHandler.Close();
					}
					return State.SendingResponse;

				case State.SendingResponse:
					ctx.Logger.Info("Sending response back to client");
					await ctx.ClientHandler.ResendResponseAsync().WithoutCapturingContext();

					//if (ctx.IsWebSocketHandshake)
					if (ctx.Request.Headers.Upgrade != null)
					{
						ctx.Logger.Verbose("WebSocket handshake");
						return State.UpgradingToWebSocketTunnel;
					}

					if (!ctx.Request.KeepAlive && !ctx.Response.KeepAlive)
					{
						ctx.Logger.Verbose("No keep-alive... closing handler");
						ctx.ClientHandler.Close();
						return State.Done;
					}

					ctx.Logger.Verbose("keep-alive!");
					await StateMachine.RunAsync(ctx.Clone()).WithoutCapturingContext();
					return State.Done;

				case State.UpgradingToWebSocketTunnel:
					ctx.Logger.Info("Upgrading connection to web socket");
					var cStream = ctx.ClientPipe.Stream;
					await ctx.EnsureConnectedToServerAsync(ctx.Request.Uri).WithoutCapturingContext();
					var sStream = ctx.ServerPipe.Stream;

					var sTask = cStream.CopyToAsync(sStream);
					var rTask = sStream.CopyToAsync(cStream);

					await Task.WhenAll(sTask, rTask).WithoutCapturingContext();
					return State.Done;

				case State.Done:
					ctx.ClientHandler?.Close();
					ctx.ServerHandler?.Close();
					return State.Done;
				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, null);
			}
			return State.Done;
		}
	}
}
