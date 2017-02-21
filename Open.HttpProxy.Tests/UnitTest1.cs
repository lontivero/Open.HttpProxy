using System;
using System.Net;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Open.HttpProxy.Tests
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod1()
		{
			var port = 9988;
			var proxy = new HttpProxy(port);
			proxy.OnRequestHeaders += (s, e) =>
			{
				Assert.AreEqual("www.google.com", e.Session.Request.Headers.Host);
			};
			proxy.Start();

			var httpClientHandler = new HttpClientHandler()
			{
				Proxy = new WebProxy(new Uri($"http://127.0.0.1:{port}")),

			};
			var c = new HttpClient(httpClientHandler);
			var res = c.GetAsync("http://www.google.com").Result;
			proxy.Stop();
		}
	}
}
