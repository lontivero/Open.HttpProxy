using System.IO;

namespace Open.HttpProxy.EventArgs
{
	public class ConnectionEventArgs : System.EventArgs
	{
		public ConnectionEventArgs(Stream stream)
		{
			Stream = stream;
		}

		public Stream Stream { get; }
	}
}