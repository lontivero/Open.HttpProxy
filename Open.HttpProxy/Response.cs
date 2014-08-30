using System;

namespace Open.HttpProxy
{
    internal class Response
    {
        private readonly Session _session;

        public Response(Session session)
        {
            _session = session;
        }

        public HTTPResponseHeaders Headers { get; set; }

        public int StatusCode
        {
            get;
            set;
        }

        public string StatusCodeDescription
        {
            get;
            set;
        }

        public byte[] ToByteArray()
        {
            return new byte[0];
        }
    }
}