using System.IO;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	internal class Pipe
	{
		public Stream Stream { get; }
		public HttpStreamReader Reader { get; }
		public HttpStreamWriter Writer { get; }

		public Pipe(Stream stream)
		{
			Stream = stream;
			Reader = new HttpStreamReader(Stream);
			Writer = new HttpStreamWriter(Stream);
		}

		public virtual Task StartAsync()
		{
			return Task.FromResult(default(object));
		}
	}
}