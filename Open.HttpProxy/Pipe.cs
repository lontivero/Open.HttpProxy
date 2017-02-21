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
			if (stream.CanTimeout)
			{
				stream.ReadTimeout = 10*1000;
				stream.WriteTimeout = 10*1000;
			}
			Stream = stream;
			Reader = new HttpStreamReader(Stream);
			Writer = new HttpStreamWriter(Stream);
		}

		public void Close()
		{
			Stream.Close();
		}
	}
}