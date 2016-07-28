using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	class SslPipe : Pipe
	{
		private readonly string _host;

		public SslPipe(Stream stream, string host)
			: base(new SslStream(stream, false))
		{
			_host = host;
		}
	}
}
