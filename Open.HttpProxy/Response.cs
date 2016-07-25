
namespace Open.HttpProxy
{
	public class Response
	{
		public StatusLine StatusLine { get; set; }
		public HttpResponseHeaders Headers { get; } = new HttpResponseHeaders();

		public byte[] Body { get; set; }

		public bool IsChunked => Headers.TransferEncoding?.Contains("chunked") ?? false;

		public bool HasBody => (Headers.ContentLength.HasValue && Headers.ContentLength.Value > 0) || IsChunked;
	}
}