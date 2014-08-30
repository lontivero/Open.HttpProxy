using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Open.HttpProxy
{
    public abstract class HttpHeaders : IEnumerable<KeyValuePair<string, string>>, IEnumerable
    {
        private readonly Dictionary<string, string> _headers;
        protected HttpHeaders()
        {
            _headers = new Dictionary<string, string>(20, StringComparer.OrdinalIgnoreCase);
        }

        public string Connection
        {
            get { return this["Connection"]; }
        }

        public int? ContentLength
        {
            get
            {
                int value;
                return  int.TryParse(this["Content-Length"], out value) ? (int?)value : null;
            }
        }

        public string ContentType
        {
            get { return this["ContentType"]; }
        }

        public string ContentMD5
        {
            get { return this["Content-MD5"]; }
        }
        public string CacheControl
        {
            get { return this["Cache-Control"]; }
        }
        public string Pragma
        {
            get { return this["Pragma"]; }
        }


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

        public byte[] ToByteArray()
        {
            var sb = new StringBuilder(50* _headers.Count);
            foreach (var header in _headers)
            {
                sb.AppendFormat("{0}: {1}\r\n", header.Key, header.Value);
            }
            return Encoding.ASCII.GetBytes(sb.ToString());
        }
    }
}