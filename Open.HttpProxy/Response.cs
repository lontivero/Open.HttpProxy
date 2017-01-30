
using System;
using System.Linq;
using System.Text;

namespace Open.HttpProxy
{
	public abstract class HttpMessage
	{
		private byte[] _body = ByteArray.Empty;
		private static readonly Encoding[] Encodings = {
			Encoding.UTF32,
			Encoding.BigEndianUnicode,
			Encoding.Unicode,
			Encoding.UTF8
		};

		public HttpHeaders Headers { get; protected set; } = new HttpHeaders();

		public byte[] Body
		{
			get { return _body; }
			set
			{
				if (_body == value) return;
				_body = value ?? ByteArray.Empty;
				if (_body.Length > 0)
				{
					Headers.Add("Content-Length", _body.Length.ToString());
				}
				else
				{
					// HEAD requests, and 1xx, 204 or 304
					//	Headers.Remove("Content-Length");
				}
			}
		}

		public Encoding BodyEncoding
		{
			get
			{
				var contentTypeHeader = Headers.ContentType ?? string.Empty;

				var contentTypes = contentTypeHeader.Split(';');
				var charsetAttr = contentTypes.FirstOrDefault(x => x.StartsWith("charset=", StringComparison.Ordinal));
				if (charsetAttr != null)
				{
					var charsetValue = charsetAttr.Substring("charset=".Length);
					if (charsetValue == "utf-8")
						charsetValue = "utf8";
					return Encoding.GetEncoding(charsetValue);
				}

				var body = Body;
				foreach (var encoding in Encodings)
				{
					var preamble = encoding.GetPreamble();
					if (body.Length < preamble.Length)
						continue;

					if (ByteArray.Compare(body, 0, preamble, 0, preamble.Length) == 0)
					{
						return encoding;
					}
				}
				return Encoding.GetEncoding("ISO-8859-1");
			}
		}

		public bool IsChunked => Headers.TransferEncoding?.Contains("chunked") ?? false;

		public bool HasBody => HasContent();

		public bool KeepAlive => MustKeepAlive();

		public bool IsWebSocketHandshake => VerifyWebSocketHandshake();

		public ProtocolVersion Version => GetVersion();

		protected abstract ProtocolVersion GetVersion();

		protected abstract bool HasContent();

		protected abstract bool VerifyWebSocketHandshake();

		private bool MustKeepAlive()
		{			
			var connHeader = Headers.Connection ?? "";
			var proxyHeader = Headers.ProxyConnection ?? "";
			var kav10 = Version.Minor == 0 && connHeader.Contains("keep-alive");
			var kav11 = Version.Minor == 1 && !connHeader.Contains("close") && !proxyHeader.Contains("close");
			return kav10 || kav11;
		}
	}

	public static class ByteArray
	{
		public static byte[] Empty = new byte[0];

		public static int Compare(byte[] arr1, int offset1, byte[] arr2, int offset2, int count)
		{
			for (var i = 0; i < count; i++)
			{
				var diff = arr1[offset1 + i] - arr2[offset2 + i];
				if (diff != 0)
				{
					return diff;
				}
			}
			return 0;
		}
	}

	public class Response : HttpMessage
	{
		public Response()
		{
		}

		public Response(StatusLine statusLine, HttpHeaders headers, byte[] body = null)
		{
			StatusLine = statusLine;
			Headers = headers;
			if(body!=null) Body = body;
		}

		public StatusLine StatusLine { get; set; }

		protected override ProtocolVersion GetVersion()
		{
			return StatusLine.Version;
		}

		protected override bool HasContent()
		{
			var code = StatusLine.Code;
			return !((code >= HttpStatusCode.Continue && code < HttpStatusCode.Ok) || code == HttpStatusCode.NoContent || code == HttpStatusCode.NotModified);
		}

		protected override bool VerifyWebSocketHandshake()
		{
			return StatusLine.Code == HttpStatusCode.SwitchingProtocols && "websocket".Equals(Headers.Upgrade, StringComparison.OrdinalIgnoreCase);
		}
	}
}