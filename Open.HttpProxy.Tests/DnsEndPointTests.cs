using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Open.HttpProxy.Tests
{
	[TestClass]
	public class DnsEndPointTests
	{
		[TestMethod]
		public void Test1()
		{
			var ep = new DnsEndPoint("www.google.com", 80);
			Assert.AreEqual(80, ep.Port);
			Assert.AreEqual("www.google.com", ep.Host);
			Assert.AreEqual("google.com", ep.Domain);
			Assert.AreEqual("*.google.com", ep.WildcardDomain);
			Assert.AreEqual("www.google.com:80", ep.ToString());
		}

		[TestMethod]
		public void Test2()
		{
			var ep = new DnsEndPoint("google.com", 443);
			Assert.AreEqual(443, ep.Port);
			Assert.AreEqual("google.com", ep.Host);
			Assert.AreEqual("google.com", ep.Domain);
			Assert.AreEqual("*.google.com", ep.WildcardDomain);
			Assert.AreEqual("google.com:443", ep.ToString());
		}

		[TestMethod]
		public void Test3()
		{
			var ep = new DnsEndPoint("digicert.tt.omtrdc.net", 443);
			Assert.AreEqual(443, ep.Port);
			Assert.AreEqual("digicert.tt.omtrdc.net", ep.Host);
			Assert.AreEqual("omtrdc.net", ep.Domain);
			Assert.AreEqual("*.tt.omtrdc.net", ep.WildcardDomain);
			Assert.AreEqual("digicert.tt.omtrdc.net:443", ep.ToString());
		}
		
	}
}
