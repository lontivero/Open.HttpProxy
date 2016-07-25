using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Open.HttpProxy
{
	public abstract class HttpHeaders : IEnumerable<KeyValuePair<string, string>>
	{
		private readonly Dictionary<string, string> _headers;
		protected HttpHeaders()
		{
			_headers = new Dictionary<string, string>(20, StringComparer.OrdinalIgnoreCase);
		}

		public string Connection => this["Connection"];

		public int? ContentLength
		{
			get
			{
				int value;
				return int.TryParse(this["Content-Length"], out value) ? (int?)value : null;
			}
		}

		public string ContentType => this["ContentType"];

		public string ContentMd5 => this["Content-MD5"];

		public string CacheControl => this["Cache-Control"];

		public string Pragma => this["Pragma"];

		public string TransferEncoding => this["Transfer-Encoding"];

		public DateTimeOffset? Date
		{
			get
			{
				var dateFormats = new[]{
					"ddd, d MMM yyyy H:m:s 'GMT'",
					"ddd, d MMM yyyy H:m:s",
					"d MMM yyyy H:m:s 'GMT'",
					"d MMM yyyy H:m:s",
					"ddd, d MMM yy H:m:s 'GMT'",
					"ddd, d MMM yy H:m:s",
					"d MMM yy H:m:s 'GMT'",
					"d MMM yy H:m:s",
					"dddd, d'-'MMM'-'yy H:m:s 'GMT'",
					"dddd, d'-'MMM'-'yy H:m:s",
					"ddd MMM d H:m:s yyyy",
					"ddd, d MMM yyyy H:m:s zzz",
					"ddd, d MMM yyyy H:m:s",
					"d MMM yyyy H:m:s zzz",
					"d MMM yyyy H:m:s"
				};
				DateTimeOffset result;
				var ok = DateTimeOffset.TryParseExact(this["Date"], dateFormats, DateTimeFormatInfo.InvariantInfo,
											 DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite |
											 DateTimeStyles.AllowInnerWhite | DateTimeStyles.AssumeUniversal, out result);
				return ok ? result : (DateTimeOffset?)null;
			}
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return _headers.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public string this[string key]
		{
			get
			{
				string value;
				return _headers.TryGetValue(key, out value) ? value : null;
			}
		}

		public void Add(string key, string value)
		{
			_headers[key] = value ?? string.Empty;
		}

		internal void AddLine(string line)
		{
			var i = line.IndexOf(':');
			Add(line.Substring(0, i), line.Substring(i + 2));
		}

		public void Remove(string headerName)
		{
			_headers.Remove(headerName);
		}
		public bool Contains(string headerName)
		{
			return this[headerName] != null;
		}

		public override string ToString()
		{
			var sb = new StringBuilder(50* _headers.Count);
			foreach (var header in _headers)
			{
				sb.AppendLine($"{header.Key}: {header.Value}");
			}
			return sb.ToString();
		}
	}
}