using System;
using System.IO;
using System.Threading.Tasks;

namespace Open.HttpProxy
{
	internal class ConnectionStream : Stream
	{
		private readonly Connection _connection;

		public ConnectionStream(Connection connection)
		{
			_connection = connection;
		}

		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _connection.ReceiveAsync(buffer, offset, count).Result;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_connection.SendAsync(buffer, offset, count).Wait();
		}

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override long Length
		{
			get { throw new NotSupportedException(); }
		}

		public override long Position 
		{
			get { throw new NotSupportedException(); } 
			set { throw new NotSupportedException(); } 
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
		{
			//if (_connection.Available)
			//{
				return !cancellationToken.IsCancellationRequested
					? _connection.ReceiveAsync(buffer, offset, count)
					: Task.FromResult(0);  //Task.FromCanceled<int>(cancellationToken);
			//}
			//return Task.FromResult(0);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
		{
			return !cancellationToken.IsCancellationRequested 
				? _connection.SendAsync(buffer, offset, count)
				: Task.FromResult(0);
		}

		public override void Close()
		{
			base.Close();
			_connection.Close();
		}
	}
}