using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Open.HttpProxy.Tests
{
	[TestClass]
	public class HttpStreamReaderTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			var mem = new MemoryStream(
				Encoding.UTF8.GetBytes("4\r\nWiki\r\n5\r\npedia\r\nE\r\n in\r\n\r\nchunks.\r\n0\r\n\r\n"));

			var reader = new HttpStreamReader(mem);
			var body = reader.ReadChunckedBodyAsync().Result;
			Assert.AreEqual("Wikipedia in\r\n\r\nchunks.", Encoding.UTF8.GetString(body));
		}

		[TestMethod]
		public void TestMethod2()
		{
			var mem =  File.ReadAllText(@".\Data\chunked.txt", Encoding.UTF8);

			var reader = new HttpStreamReader(new MemoryStream(Encoding.UTF8.GetBytes(mem)));
			var body = Encoding.UTF8.GetString(reader.ReadChunckedBodyAsync().Result);

			var expected = File.ReadAllText(@".\Data\processed.txt", Encoding.UTF8);
			Assert.AreEqual(expected, body);
		}
	}
}
