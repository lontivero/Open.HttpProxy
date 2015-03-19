using Open.Tcp;

namespace Open.HttpProxy.EventArgs
{
    public class ConnectionEventArgs : System.EventArgs
    {
        private readonly Connection _connection;

        public ConnectionEventArgs(Connection connection)
        {
            _connection = connection;
        }

        public Connection Connection
        {
            get { return _connection; }
        }
    }
}