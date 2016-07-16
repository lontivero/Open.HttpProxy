using System;
using Open.HttpProxy;

namespace ProxyTest
{
	class Program
	{
		static void Main(string[] args)
		{
			var httpProxy = new HttpProxy();
			httpProxy.Start();
			Console.ReadKey();
		}
	}
}
