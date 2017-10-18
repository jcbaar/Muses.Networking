using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Muses.Networking
{
    /// <summary>
    /// Class representing a simple TCP/IP server.
    /// </summary>
    public sealed class TcpIpServer : IDisposable
    {
        #region Private data
        TcpListener _listener;
        readonly ITcpIpServiceProvider _provider;
        readonly List<TcpIpSocket> _connections;
        readonly int _port;
        readonly ReaderWriterLockSlim _lock;
        #endregion

        #region Construction/destruction
        /// <summary>
        /// Initializes server. To start accepting connections call Start method.
        /// </summary>
        public TcpIpServer(ITcpIpServiceProvider provider, int port)
        {

            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) throw new ArgumentOutOfRangeException(nameof(port));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _port = port;
            _connections = new List<TcpIpSocket>();
            _lock = new ReaderWriterLockSlim();
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Start accepting connections.
        /// </summary>
        /// <returns>True if the server was started. False if the server was already started.</returns>
        public bool Start()
        {
            if (_listener == null)
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _listener.BeginAcceptSocket(ConnectionReadyHandler, null);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Shutdown the server
        /// </summary>
        /// <returns>True if the server was stopped. False if the server was already stopped.</returns>
        public bool Stop()
        {
            // Close the server socket.
            if (_listener != null)
            {
                _listener.Stop();
                _listener = null;

                try
                {
                    _lock.EnterWriteLock();
                    _connections.ForEach(s => DropConnection(s));
                    _connections.Clear();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Broadcast a message to all connected clients.
        /// </summary>
        /// <param name="msg">The message to forward.</param>
        public void BroadcastMessage(byte[] msg)
        {
            BroadcastMessage(msg, 0, msg.Length);
        }

        /// <summary>
        /// Broadcast a message to all connected clients.
        /// </summary>
        /// <param name="msg">The message to forward.</param>
        /// <param name="offset">The offset in the buffer.</param>
        /// <param name="length">The number of bytes to send.</param>
        public void BroadcastMessage(byte[] msg, int offset, int length)
        {
            try
            {
                _lock.EnterReadLock();
                _connections.ForEach( socket => socket.Write(msg, offset, length));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        #endregion

        #region Private callbacks
        /// <summary>
        /// Callback function: A new connection is waiting.
        /// </summary>
        private void ConnectionReadyHandler(IAsyncResult ar)
        {
            if (_listener == null)
            {
                return;
            }

            TcpIpSocket socket = null;
            try
            {
                // Accept the incoming connection request.
                TcpClient conn;
                try
                {
                    conn = _listener.EndAcceptTcpClient(ar);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                socket = new TcpIpSocket(conn, _provider);

                try
                { 
                    _lock.EnterUpgradeableReadLock();
                    if (MaxConnections > 0 && _connections.Count >= MaxConnections)
                    {
                        bool allowAnyway = false;
                        socket.Provider.OnMaxConnectionsReached(socket, ref allowAnyway);

                        if (allowAnyway == false)
                        {
                            DropConnection(socket);
                            return;
                        }
                    }

                    try
                    {
                        _lock.EnterWriteLock();
                        _connections.Add(socket);
                        socket.Provider.OnConnected(socket);
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                }

                socket.Provider.OnConnected(socket);

                // Queue the IO receive job for the new connection.
                socket.Stream.BeginRead(socket.Buffer, 0, socket.Buffer.Length, ReceivedDataReadyHandler, socket);
                _listener?.BeginAcceptTcpClient(ConnectionReadyHandler, null);
            }
            catch (ThreadAbortException)
            {
                DropConnection(socket);
            }
            catch (Exception ex)
            {
                _provider?.OnException(socket, ex);
                DropConnection(socket);
            }
        }

        /// <summary>
        /// Executes OnReceiveData method from the service provider.
        /// </summary>
        private void ReceivedDataReadyHandler(IAsyncResult ar)
        {
            var socket = ar.AsyncState as TcpIpSocket;
            try
            {
                if (socket?.Socket != null)
                {
                    try
                    {
                        socket.Stream.EndRead(ar);
                    }
                    catch(ObjectDisposedException)
                    {
                        return;
                    }

                    // 0 bytes available signals us that the remote host
                    // has closed the connection.
                    if (socket.AvailableData == 0)
                    {
                        DropConnection(socket);
                    }
                    else
                    {
                        socket.HasData = true;
                        socket.Provider.OnReceiveData(socket);

                        socket.Stream.BeginRead(socket.Buffer, 0, socket.Buffer.Length, ReceivedDataReadyHandler, socket);
                    }
                }
            }
            catch (SocketException se)
            {
                // WSAECONNRESET?
                if (se.NativeErrorCode.Equals(10054) == false)
                {
                    socket?.Provider.OnException(socket, se);
                }
                else
                {
                    DropConnection(socket);
                }
            }
            catch (ThreadAbortException)
            {
                DropConnection(socket);
            }
            catch (Exception ex)
            {
                if (socket != null)
                {
                    socket.Provider.OnException(socket, ex);
                    DropConnection(socket);
                }
            }
        }
        #endregion

        #region Internal methods
        /// <summary>
        /// Removes a connection from the list and cleans up it's
        /// socket.
        /// </summary>
        internal void DropConnection(TcpIpSocket socket)
        {
            if(socket == null)
            {
                return;
            }

            try
            {
                try
                {
                    _lock.EnterWriteLock();
                    if (_connections.Contains(socket))
                    {
                        _connections.Remove(socket);
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                socket.Provider.OnClosingConnection(socket);
                socket.Close();
            }
            catch (Exception ex)
            {
                socket?.Provider?.OnException(socket, ex);
            }
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Gets or sets the maximum number of connections which are served.
        /// Settings this value to less than the number of existing client
        /// connections will not close any of the existing connections.
        /// </summary>
        public int MaxConnections { get; set; } = 0;

        /// <summary>
        /// Gets the number of connections which are currently being served.
        /// </summary>
        public int CurrentConnections
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _connections.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
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
                    try
                    {
                        _lock.EnterWriteLock();
                        _connections.ForEach(s =>
                        {
                            s.Close();
                        });
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }

                    if(_listener != null)
                    {
                        _listener.Stop();
                        _listener = null;
                    }
                    if (_lock != null) _lock.Dispose();
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
