using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Open.HttpProxy.Utils;

namespace Open.HttpProxy.BufferManager
{
	public sealed class ManualBufferedStream : Stream 
	{
		private readonly Stream _s;
		private readonly BufferAllocator _allocator;
		private ArraySegment<byte> _buffer; 
		private int _readPos;
		private int _readLen;
		private int _writePos;
		private readonly int _bufferSize;


		public ManualBufferedStream(Stream stream, BufferAllocator allocator)
			: this(stream, allocator, BufferAllocator.BlockSize)
		{
		}

		public ManualBufferedStream(Stream stream, BufferAllocator allocator, int bufferSize)
		{
			_s = stream;
			_allocator = allocator;
			_bufferSize = bufferSize;
		}

		public override bool CanRead => _s.CanRead;

		public override bool CanWrite => _s.CanWrite;

		public override bool CanSeek => false;

		public override long Length 
		{
			get 
			{
				throw new NotSupportedException();
			}
		}

		public override long Position 
		{
			get {
				return _s.Position + (_readPos - _readLen + _writePos);
			}
			set {
				throw new NotSupportedException();
			}
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (!disposing) return;
				// TODO: and....
			}
			finally
			{
				_s.Close();
				_allocator.Free(_buffer);

				base.Dispose(disposing);
			}
		}

		public override void Flush()
		{
			throw new NotSupportedException();
		}

		public override async Task FlushAsync(CancellationToken ct)
		{
			if (_writePos > 0) 
				await FlushWriteAsync().ConfigureAwait(continueOnCapturedContext: false);
			else if (_readPos < _readLen && _s.CanSeek) 
				FlushRead();
			_readPos = 0;
			_readLen = 0;
		}

		private void FlushRead()
		{
			_readPos = 0;
			_readLen = 0;
		}

		private async Task FlushWriteAsync()
		{
			await _s.WriteAsync(_buffer.Array,  _buffer.Offset, _writePos).ConfigureAwait(continueOnCapturedContext: false);
			_writePos = 0;
			await _s.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
 
		public override async Task<int> ReadAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
		{
			await EnsuranceBuffer().ConfigureAwait(continueOnCapturedContext: false);

			var available = _readLen - _readPos;
			var readCount = Math.Min(available, count);
			if (available > 0)
			{
				Buffer.BlockCopy(_buffer.Array, _buffer.Offset + _readPos, array, offset, readCount);
				available -= readCount;
				_readPos += readCount;
			}
			var n = await _s.ReadAsync(array, offset + readCount, count - readCount, cancellationToken).WithoutCapturingContext();

			return readCount + n;
		}

		public override async Task WriteAsync(byte[] array, int offset, int count, CancellationToken ct)
		{
			await EnsuranceBuffer().ConfigureAwait(continueOnCapturedContext: false);
			var freeBuffer = _bufferSize - _writePos;
			var writeToBuffer = count < freeBuffer;
			if (writeToBuffer)
			{
				Buffer.BlockCopy(array, offset, _buffer.Array, _buffer.Offset + _writePos, count);
				_writePos += count;
				return;
			}

			if (_writePos > 0)
			{
				await FlushWriteAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			await _s.WriteAsync(array, offset, count, ct).ConfigureAwait(continueOnCapturedContext: false);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
		private async Task EnsuranceBuffer()
		{
			if (_buffer.Count == 0)
			{
				_buffer = await _allocator.AllocateAsync(_bufferSize).WithoutCapturingContext();
			}
		}
	}
}