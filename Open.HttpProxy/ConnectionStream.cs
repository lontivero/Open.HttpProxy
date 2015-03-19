using System;
using System.IO;
using System.Threading.Tasks;
using Open.Tcp;

namespace Open.HttpProxy
{
    class ConnectionStream : Stream
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
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

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
            if (!cancellationToken.IsCancellationRequested)
            {
                return _connection.ReceiveAsync(buffer, offset, count);
            }
            return TaskUtils.CancelledTask<int>();
        }


        public override Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                return _connection.SendAsync(buffer, offset, count);
            }
            return TaskUtils.CancelledTask<int>();
        }

        public override void Close()
        {
            base.Close();
            _connection.Close();
        }
    }

    static class TaskUtils
    {
        public static Task<T> CancelledTask<T>()
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetCanceled();
            return tcs.Task;
        }
    }
}