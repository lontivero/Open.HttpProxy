namespace Open.HttpProxy
{
	public class SessionEventArgs : System.EventArgs
	{
		public Session Session { get; }

		public SessionEventArgs(Session session)
		{
			Session = session;
		}
	}
}