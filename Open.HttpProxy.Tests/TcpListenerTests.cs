using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Open.HttpProxy.Tests
{
	[TestClass]
	public class TcpListenerTests
	{
		private long connectionCount;
		private ManualResetEvent done = new ManualResetEvent(false);
		const int connections = 100 * 100;

		[TestMethod]
		public void NoLossConnections()
		{
			var listener = new Open.HttpProxy.Listeners.TcpListener(8899);
			listener.ConnectionRequested += Listener_ConnectionRequested;
			listener.Start();

			for (int i = 0; i < connections; i++)
			{
				Task.Run(() =>
				{
					var c = new TcpClient();
					c.Connect(new IPEndPoint(IPAddress.Loopback, 8899));
				});
			}
			done.WaitOne();
			Assert.AreEqual(connections, connectionCount);
		}

		private void Listener_ConnectionRequested(object sender, EventArgs.ConnectionEventArgs e)
		{
			if (Interlocked.Increment(ref connectionCount) == connections) done.Set();
		}
	}
}
