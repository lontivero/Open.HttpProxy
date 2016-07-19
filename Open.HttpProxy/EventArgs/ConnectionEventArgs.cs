namespace Open.HttpProxy.EventArgs
{
	public class ConnectionEventArgs : System.EventArgs
	{
		public ConnectionEventArgs(Connection connection)
		{
			Connection = connection;
		}

		public Connection Connection { get; }
	}
}