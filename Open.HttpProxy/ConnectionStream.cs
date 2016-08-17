using System;
using System.IO;
using System.Threading;
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

		public override bool CanRead => _connection.IsConnected;

		public override bool CanSeek => false;

		public override bool CanWrite => _connection.IsConnected;

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
			return !cancellationToken.IsCancellationRequested
				? _connection.ReceiveAsync(buffer, offset, count)
				: Task.FromResult(0);  //Task.FromCanceled<int>(cancellationToken);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
		{
			return !cancellationToken.IsCancellationRequested 
				? _connection.SendAsync(buffer, offset, count)
				: Task.FromResult(0);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _connection.Receive(buffer, offset, count);
		}
		public override void Write(byte[] buffer, int offset, int count)
		{
			_connection.Send(buffer, offset, count);
		}

		public override void Close()
		{
			base.Close();
			_connection.Close();
		}
	}
}