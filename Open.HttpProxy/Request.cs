using System;
using System.IO;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	public class Request
	{
		public RequestLine RequestLine { get; set; }

		public HttpRequestHeaders Headers { get; } = new HttpRequestHeaders();

		public bool IsHttps => RequestLine.Verb == "CONNECT";

		public bool KeepAlive => Headers.Connection?.Contains("keep-alive") ?? false;
		public bool IsChunked => Headers.TransferEncoding?.Contains("chunked") ?? false;

		public byte[] Body { get; set; }

		internal Uri GetUriFromRequest()
		{
			var requestUri = RequestLine.Authority;
			var requestHost = Headers.Host;
			if (requestUri == "*")
			{
				return new Uri(requestHost, UriKind.Relative);
			}
			if (Uri.IsWellFormedUriString(requestUri, UriKind.Absolute))
			{
				return new Uri(requestUri, UriKind.Absolute);
			}
			if (Uri.IsWellFormedUriString(requestUri, UriKind.Relative))
			{
				return new Uri(new Uri(requestHost), requestUri);
			}
			throw new Exception($"Error: invalid {requestUri}");
		}

		//public async Task<Stream> GetContentStreamAsync()
		//{
		//	var contentLenght = _session.Request.Headers.ContentLength;
		//	if( !contentLenght.HasValue || contentLenght.Value == 0) return new MemoryStream(0);
		//	return await _session.ClientHandler.GetRequestStreamAsync(contentLenght.Value);
		//}
	}
}