using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Muses.Networking
{
    public sealed class TcpIpClient : IDisposable
    {
        #region Private data
        readonly ITcpIpServiceProvider _provider;
        #endregion

        public TcpIpSocket Socket { get; private set; }

        #region Construction/destruction
        /// <summary>
        /// Constructor. Creates a TcpIpClient and set's its
        /// parameters.
        /// </summary>
        /// <param name="provider">The provider to handle incoming messages.</param>
        public TcpIpClient(ITcpIpServiceProvider provider)
        {
            // Setup the object.
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Creates a socket and connects it to the server. The method will
        /// not return until either the connection was established or an
        /// error occurs.
        /// </summary>
        /// <param name="address">The IP address to connect to.</param>
        /// <param name="port">The port number to connect to.</param>
        public void Connect(string address, int port)
        {
            if (Socket != null) throw new InvalidOperationException("TcpIpClient already connected.");
            try
            {
                IPAddress addr = IPAddress.Parse(address);

                var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                clientSocket.Connect(new IPEndPoint(addr, port));

                Socket = new TcpIpSocket(clientSocket, _provider);

                _provider.OnConnected(Socket);
                Socket.Socket.BeginReceive(Socket.Buffer, 0, Socket.Buffer.Length, SocketFlags.None, ReceivedDataReadyHandler, Socket);
            }
            catch (Exception)
            {
                Socket?.Close();
                Socket = null;
                throw;
            }
        }

        /// <summary>
        /// If the socket is valid the OnDropConnection() method of the provider
        /// is called. Then the socket is shutdown and closed. On an error the
        /// exception is passed on to the OnException() method of the provider.
        /// </summary>
        /// <param name="callProvider">True to call the provider it's <see cref="ITcpIpServiceProvider.OnClosingConnection(TcpIpSocket)"/>
        /// method before closing the socket.</param>
        public void Disconnect(bool callProvider = false)
        {
            if (Socket == null) throw new InvalidOperationException("TcpIpClient not connected.");

            if (callProvider)
            {
                Socket.Provider.OnClosingConnection(Socket);
            }
            Socket.Close();
            Socket = null;
        }
        #endregion

        #region Private handlers
        /// <summary>
        /// Handles incoming data on the socket. On success this method will call the
        /// OnReceivedData() method of the object it's provider to further handle the
        /// incoming data. On error it will pass on the exception to the OnException()
        /// method of the provider.
        /// </summary>
        /// <param name="ar">IAsyncResult object containing information about the
        /// asynchronous method call.</param>
        private void ReceivedDataReadyHandler(IAsyncResult ar)
        {
            try
            {
                if (Socket?.Socket != null)
                {
                    try
                    {
                        Socket.Socket.EndReceive(ar);
                    }
                    catch(ObjectDisposedException)
                    {
                        Socket.Dispose();
                        Socket = null;
                        return;
                    }

                    if (Socket.AvailableData == 0)
                    {
                        Disconnect(true);
                    }
                    else
                    {
                        Socket.HasData = true;
                        Socket.Provider.OnReceiveData(Socket);
                        Socket.Socket.BeginReceive(Socket.Buffer, 0, Socket.Buffer.Length, SocketFlags.None, ReceivedDataReadyHandler, Socket);
                    }
                }
            }
            catch (SocketException se)
            {
                // WSAECONNRESET?
                if (se.NativeErrorCode.Equals(10054) == false)
                {
                    Socket.Provider.OnException(Socket, se);
                }
                else
                {
                    Disconnect(true);
                }
            }
            catch (ThreadAbortException)
            {
                Disconnect(true);
            }
            catch (Exception ex)
            {
                Socket?.Provider?.OnException(Socket, ex);
                Disconnect(true);
            }
        }
        #endregion

        #region IDisposable Support
        private bool _disposedValue;

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (Socket != null) Socket.Dispose();
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
