namespace Open.Tcp.EventArgs
{
    public class DataEventArgs : ConnectionEventArgs
    {
        private readonly byte[] _data;

        public DataEventArgs(Connection connection, byte[] data)
            :base(connection)
        {
            _data = data;
        }

        public byte[] Data
        {
            get { return _data; }
        }
    }
}