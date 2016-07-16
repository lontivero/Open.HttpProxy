
namespace Open.HttpProxy
{
	internal class Response
	{
		private readonly Session _session;

		public Response(Session session)
		{
			_session = session;
		}

		public StatusLine StatusLine { get; set; }
		public HTTPResponseHeaders Headers { get; set; }

		public string Body { get { return string.Empty; } }
	}
}