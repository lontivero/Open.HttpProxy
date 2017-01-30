using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.HttpProxy.BufferManager;

namespace Open.HttpProxy.Tests
{
	[TestClass]
	public class BufferAllocatorTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			var alloc = new BufferAllocator(new byte[8*1024*40]);
			var consumers = new Task[100];
			for(var i=0; i < consumers.Length; i++)
			{
				var consumer = Task.Run(async () =>
				{
					var buffer = alloc.AllocateAsync(new Random().Next(4 * 1024, 32 * 1024));
					try
					{
						if (buffer.Array[buffer.Offset] == 0xff) throw new Exception("the same buffer was asigned");
						buffer.Array[buffer.Offset] = 0xff;
						await Task.Delay(200);
					}
					finally
					{
						buffer.Array[buffer.Offset] = 0x00;
						alloc.Free(buffer);
					}
				});
				consumers[i] = consumer;
			}
			Task.WaitAll(consumers);
		}
	}
}
