using System.IO;
using System.Threading.Tasks;
using Open.Tcp.BufferManager;

namespace Open.HttpProxy
{
    internal class Request
    {
        private readonly Session _session;
        private readonly HttpRequestHeaders _headers = new HttpRequestHeaders();

        public string RequestLine { get; private set; }

        public Request(Session session)
        {
            _session = session;
        }

        public void SetRequestLine(string line)
        {
            RequestLine = line;
            var ifs = line.IndexOf(' ');
            var ils = line.LastIndexOf(' ');
            Verb = line.Substring(0, ifs);
            Uri = line.Substring(ifs + 1, ils - ifs-1);
            Version = line.Substring(ils + 1);
        }

        public string Verb { get; private set; }
        public string Uri { get; private set; }
        public string Version { get; private set; }

        internal void AddHeader(string line)
        {
            var i = line.IndexOf(':');
            _headers.Add(line.Substring(0, i), line.Substring(i + 2));
        }

        public HttpRequestHeaders Headers
        {
            get { return _headers; }
        }

        public async Task<Stream> GetContentStreamAsync()
        {
            var contentLenght = _session.Request.Headers.ContentLength;
            if( !contentLenght.HasValue || contentLenght.Value == 0) return new MemoryStream(0);
            return await _session.ClientHandler.GetRequestStreamAsync(contentLenght.Value);
        }
    }
}