using System.IO;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	public class Request
	{
		private readonly Session _session;

		public RequestLine RequestLine { get; set; }

		public Request(Session session)
		{
			_session = session;
		}

		public HttpRequestHeaders Headers { get; } = new HttpRequestHeaders();

		public string Body { get; set; }

		public async Task<Stream> GetContentStreamAsync()
		{
			var contentLenght = _session.Request.Headers.ContentLength;
			if( !contentLenght.HasValue || contentLenght.Value == 0) return new MemoryStream(0);
			return await _session.ClientHandler.GetRequestStreamAsync(contentLenght.Value);
		}
	}

	public class RequestLine
	{
		public string Verb { get; private set; }
		public string Uri { get; private set; }
		public string Version { get; private set; }

		public RequestLine(string line)
		{
			var ifs = line.IndexOf(' ');
			var ils = line.LastIndexOf(' ');
			Verb = line.Substring(0, ifs);
			Uri = line.Substring(ifs + 1, ils - ifs-1);
			Version = line.Substring(ils + 1);
		}

		public override string ToString()
		{
			return $"{Verb} {Uri} {Version}";
		}
	}
}