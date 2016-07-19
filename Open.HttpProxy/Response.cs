
namespace Open.HttpProxy
{
	public class Response
	{
		private readonly Session _session;

		public Response(Session session)
		{
			_session = session;
		}

		public StatusLine StatusLine { get; set; }
		public HttpResponseHeaders Headers { get; set; }

		public string Body => string.Empty;
	}
}