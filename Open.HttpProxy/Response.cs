
namespace Open.HttpProxy
{
	public class Response
	{
		public StatusLine StatusLine { get; set; }
		public HttpResponseHeaders Headers { get; } = new HttpResponseHeaders();

		public byte[] Body { get; set; }

		public bool IsChunked => Headers.TransferEncoding?.Contains("chunked") ?? false;

		public bool HasBody => (Headers.ContentLength.HasValue && Headers.ContentLength.Value > 0) || IsChunked;

		public bool KeepAlive => MustKeepAlive();

		private bool MustKeepAlive()
		{
			var ver = StatusLine.Version;
			var connHeader = Headers.Connection ?? "";
			var kav10 = ver.Minor==0 && connHeader.Contains("keep-alive");
			var kav11 = ver.Minor==1 && !connHeader.Contains("close");
			return kav10 || kav11;
		}
	}
}