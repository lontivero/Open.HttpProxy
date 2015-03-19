using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Open.HttpProxy.BufferManager
{
    public sealed class ManualBufferedStream : Stream {
        private readonly Stream _s;         
        private readonly BufferAllocator _allocator;
        private ArraySegment<byte> _buffer; 
        private int _readPos;   
        private int _readLen;   
        private int _writePos;  
        private int _bufferSize;
 
  
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
  
        public override bool CanRead 
        {
            get { return _s.CanRead; }
        }
  
        public override bool CanWrite 
        {
            get { return _s.CanWrite; }
        }
 
        public override bool CanSeek 
        {
            get { return false; }
        }
 
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
            }
            finally {
                _s.Close();
                _allocator.Free(_buffer);
 
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override async Task FlushAsync(CancellationToken ct) {
            if (_writePos > 0) 
                await FlushWriteAsync();
            else if (_readPos < _readLen && _s.CanSeek) 
                FlushRead();
            _readPos = 0;
            _readLen = 0;
        }
  
        private void FlushRead() {
            _readPos = 0;
            _readLen = 0;
        }
 
        private async Task FlushWriteAsync() {
            await _s.WriteAsync(_buffer.Array,  _buffer.Offset, _writePos);
            _writePos = 0;
            await _s.FlushAsync();
        }
 
        public override async Task<int> ReadAsync(byte[] array, int offset, int count, CancellationToken cancellationToken) {
            int n = _readLen - _readPos;
            if (n == 0) {
                if (_writePos > 0) await FlushWriteAsync();
                if (count > _bufferSize) {
                    n = _s.Read(array, offset, count);
                    // Throw away read buffer.
                    _readPos = 0;
                    _readLen = 0;
                    return n;
                }
                if (_buffer.Count == 0) _buffer = await _allocator.AllocateAsync(_bufferSize);
                n = await _s.ReadAsync(_buffer.Array, _buffer.Offset, _buffer.Count);
                if (n == 0) return 0;
                _readPos = 0;
                _readLen = n;
            }
            if (n > count) n = count;
            Buffer.BlockCopy(_buffer.Array, _buffer.Offset + _readPos, array, offset, n);
            _readPos += n;
  
            return n;
        }
   
        public override async Task WriteAsync(byte[] array, int offset, int count, CancellationToken ct) {
            if (_writePos==0) {
                if (_readPos < _readLen)
                    FlushRead();
                else {
                    _readPos = 0;
                    _readLen = 0;
                }
            }
 
            if (_writePos > 0) {
                int numBytes = _bufferSize - _writePos;   // space left in buffer
                if (numBytes > 0) {
                    if (numBytes > count)
                        numBytes = count;
                    Buffer.BlockCopy(array, offset, _buffer.Array, _buffer.Offset + _writePos, numBytes);
                    _writePos += numBytes;
                    if (count==numBytes) return;
                    offset += numBytes;
                    count -= numBytes;
                }
                // Reset our buffer.  We essentially want to call FlushWrite
                // without calling Flush on the underlying Stream.
                await _s.WriteAsync(_buffer.Array, _buffer.Offset, _writePos);
                _writePos = 0;
            }
            if (count >= _bufferSize) {
                await _s.WriteAsync(array, offset, count);
                return;
            }
            
            if (count == 0)
                return;

            if (_buffer.Count == 0) _buffer = await _allocator.AllocateAsync(_bufferSize);
            Buffer.BlockCopy(array, offset, _buffer.Array, _buffer.Offset, count);
            _writePos = count;
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
            throw new NotImplementedException();
        }
    }
}