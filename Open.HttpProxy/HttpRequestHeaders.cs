using System;

namespace Open.HttpProxy
{
    class HttpRequestHeaders : HttpHeaders
    {
        public string Host { get { return this["Host"]; }}

        public string Accept { get { return this["Accept"]; } }
        public Uri Referer { get { return new Uri(this["Referer"]); } }
        public string AcceptCharset { get { return this["Accept-Charset"]; } }
        public string AcceptEncoding { get { return this["Accept-Encoding"]; } }
        public string AcceptLanguage { get { return this["Accept-Language"]; } }
        public string Authorization { get { return this["Authorization"]; } }
        public string Expect { get { return this["Expect"]; } }
        public bool? ExpectContinue 
        { 
            get { 
                bool val = false;
                return bool.TryParse(this["Expect-Continue"], out val) && val; 
            } 
        }
    }

    class HTTPResponseHeaders : HttpHeaders
    {
    }
}