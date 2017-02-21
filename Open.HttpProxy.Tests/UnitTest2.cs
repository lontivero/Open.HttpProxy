using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Open.HttpProxy.Tests
{
	[TestClass]
	public class UnitTest2
	{
		[TestMethod]
		public void MissingHostHeader()
		{
			var inStream = new MemoryStream(Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\n"));
			var outStream = new MemoryStream();
			var proxy = new HttpProxy();
			proxy.OnRequestHeaders += (s, args) => Assert.Fail("Event invoked when no valid request");
			var session = new Session(new Pipe(inStream), new Pipe(outStream), proxy);
			StateMachine.RunAsync(session).RunSynchronously();
			Assert.AreEqual("GET", session.Request.RequestLine.Verb);
			Assert.AreEqual("/", session.Request.RequestLine.Uri);
			Assert.AreEqual("HTTP/1.1", session.Request.RequestLine.Version.ToString());
			Assert.IsNull(session.Request.Headers.Host);
		}

		[TestMethod]
		public void TestMethod1()
		{
			var inStream = new MemoryStream(Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nHost: www.google.com\r\n\r\n"));
			var outStream = new MemoryStream();
			var proxy = new HttpProxy();
			var eventRaised = false;
			proxy.OnRequestHeaders += (s, args) => eventRaised=true;
			var session = new Session(new Pipe(inStream), new Pipe(outStream), proxy);
			StateMachine.RunAsync(session).RunSynchronously();
			Assert.AreEqual("GET", session.Request.RequestLine.Verb);
			Assert.AreEqual("/", session.Request.RequestLine.Uri);
			Assert.AreEqual("HTTP/1.1", session.Request.RequestLine.Version.ToString());
			Assert.AreEqual("www.google.com", session.Request.Headers.Host);
			Assert.IsTrue(eventRaised);
		}
	}
}
