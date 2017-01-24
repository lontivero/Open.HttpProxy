using System.IO;
using System.Net.Sockets;

namespace Open.HttpProxy.EventArgs
{
	public class ConnectionEventArgs : System.EventArgs
	{
		public ConnectionEventArgs(Socket socket)
		{
			socket.LingerState = new LingerOption(true, 0);
			Socket = socket;
			Stream = new NetworkStream(socket, true);
		}

		public Socket Socket { get; }

		public Stream Stream { get; }
	}
}