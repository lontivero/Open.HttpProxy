using System;

namespace Open.HttpProxy
{
	public class HttpRequestHeaders : HttpHeaders
	{
		public string Host => this["Host"];

		public string Accept => this["Accept"];

		public Uri Referer => new Uri(this["Referer"]);

		public string AcceptCharset => this["Accept-Charset"];

		public string AcceptEncoding => this["Accept-Encoding"];

		public string AcceptLanguage => this["Accept-Language"];

		public string Authorization => this["Authorization"];

		public string Expect => this["Expect"];

		public bool? ExpectContinue 
		{
			get
			{
				bool val;
				return bool.TryParse(this["Expect-Continue"], out val) && val;
			}
		}
	}

	public class HttpResponseHeaders : HttpHeaders
	{
	}
}