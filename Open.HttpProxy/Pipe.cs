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
			Reader = new HttpStreamReader(new BufferedStream(Stream));
			Writer = new HttpStreamWriter(new BufferedStream(Stream));
		}

		public void Close()
		{
			Reader.Close();
			Writer.Close();
			Stream.Close();
		}
	}
}