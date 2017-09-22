using System;
using System.Text;
using System.Threading;

namespace Muses.Networking.Tests
{
    public class TestServiceProvider : ITcpIpServiceProvider, IDisposable
    {
        public TestServiceProvider(bool isServer = false)
        {
            IsServerProvider = isServer;
        }

        public bool IsServerProvider { get; set; }

        public bool ConnectedCalled { get; set; }
        public AutoResetEvent ConnectedEvent { get; set; } = new AutoResetEvent(false);
        public bool ClosingCalled { get; set; }
        public AutoResetEvent ClosingEvent { get; set; } = new AutoResetEvent(false);
        public bool MaxConnectionsCalled { get; set; }
        public AutoResetEvent MaxConnmectionsEvent { get; set; } = new AutoResetEvent(false);
        public bool ReceiveDataCalled { get; set; }
        public AutoResetEvent ReceiveEvent { get; set; } = new AutoResetEvent(false);
        public bool ExceptionCalled { get; set; }
        public AutoResetEvent ExceptionEvent { get; set; } = new AutoResetEvent(false);

        public string ReceivedData { get; set; }

        public void OnClosingConnection(TcpIpSocket socket)
        {
            ClosingCalled = true;
            ClosingEvent?.Set();
        }

        public void OnConnected(TcpIpSocket socket)
        {
            ConnectedCalled = true;
            ConnectedEvent?.Set();
        }

        public void OnException(TcpIpSocket socket, Exception ex)
        {
            ExceptionCalled = true;
            ExceptionEvent?.Set();
        }

        public void OnMaxConnectionsReached(TcpIpSocket socket, ref bool allowAnyway)
        {
            MaxConnectionsCalled = true;
            MaxConnmectionsEvent?.Set();
        }

        public void OnReceiveData(TcpIpSocket socket)
        {
            var buffer = new byte[4096];
            var sb = new StringBuilder();
            int len;
            while((len = socket.Read(buffer)) > 0)
            {
                sb.Append(Encoding.ASCII.GetString(buffer, 0, len));
            }
            ReceivedData = sb.ToString();
            ReceiveDataCalled = true;
            ReceiveEvent?.Set();

            if(IsServerProvider)
            {
                // Echo back.
                socket.Write(Encoding.ASCII.GetBytes(sb.ToString()));
            }
        }

        #region IDisposable Support
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (ConnectedEvent != null) ConnectedEvent.Dispose();
                    if (ClosingEvent != null) ClosingEvent.Dispose();
                    if (MaxConnmectionsEvent != null) MaxConnmectionsEvent.Dispose();
                    if (ReceiveEvent != null) ReceiveEvent.Dispose();
                    if (ExceptionEvent != null) ExceptionEvent.Dispose();
                    ConnectedEvent = null;
                    ClosingEvent = null;
                    MaxConnmectionsEvent = null;
                    ReceiveEvent = null;
                    ExceptionEvent = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
