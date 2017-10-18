using System;
using System.Net;
using System.Net.Sockets;

namespace Muses.Networking
{
    /// <summary>
    /// Socket class.
    /// </summary>
    public sealed class TcpIpSocket : IDisposable
    {
        #region Construction.
        /// <summary>
        /// Constructor. Initializes an instance of the object.
        /// </summary>
        /// <param name="socket">The <see cref="TcpClient"/>.</param>
        /// <param name="provider">The <see cref="ITcpIpServiceProvider"/> instance that
        /// handles this socket.</param>
        internal TcpIpSocket(TcpClient socket, ITcpIpServiceProvider provider)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            if (IsConnected)
            {
                Stream = Socket.GetStream();
            }
        }
        #endregion

        #region Internal properties
        /// <summary>
        /// Gets the <see cref="ITcpIpServiceProvider"/> which handles this connection.
        /// </summary>
        internal ITcpIpServiceProvider Provider { get; private set; }

        /// <summary>
        /// Gets the actual <see cref="Socket"/> for this connection. 
        /// </summary>
        internal TcpClient Socket { get; private set; }

        /// <summary>
        /// Gets the <see cref="NetworkStream"/> associated with the <see cref="TcpClient"/>
        /// </summary>
        internal NetworkStream Stream { get; private set; }

        /// <summary>
        /// Gets the internal buffer.
        /// </summary>
        internal byte[] Buffer { get; private set; } = new byte[1];

        /// <summary>
        /// Gets or sets the flag indication if the internal IO buffer
        /// contains any unread data.
        /// </summary>
        internal bool HasData { get; set; }
        #endregion

        #region Public properties
        /// <summary>
        /// Gets the network address of the remote socket when the
        /// socket is connected.
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get => Socket?.Client.RemoteEndPoint as IPEndPoint;
        }

        /// <summary>
        /// Gets the number of bytes waiting to be read.
        /// </summary>
        public int AvailableData
        {
            get => Socket?.Available ?? 0;
        }

        /// <summary>
        /// Gets the current connected state. The returned connected state is the 
        /// actual connection state at the time of this call.
        /// NOTE: This performs a zero-byte send on the underlying socket.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if(Socket == null)
                {
                    return false;
                }

                // Save the current blocking state of the socket.
                bool blockingState = Socket.Client.Blocking;
                try
                {
                    // Perform a non-blocking, 0-byte send over the socket. If it succeeds
                    // or throws a 10035 (WSAEWOULDBLOCK) error the socket is still
                    // connected.
                    byte[] tmp = new byte[1];
                    Socket.Client.Blocking = false;
                    Socket.Client.Send(tmp, 0, 0);
                    return true;
                }
                catch (SocketException e)
                {
                    // 10035 == WSAEWOULDBLOCK == still connected.
                    if (e.NativeErrorCode.Equals(10035))
                    {
                        return true;
                    }
                    return false;
                }
                finally
                {
                    // Restore the original blocking state.
                    Socket.Client.Blocking = blockingState;
                }
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Reads from the socket up to a maximum of <paramref name="buffer"/> length
        /// bytes.
        /// </summary>
        /// <param name="buffer">The buffer into which the data should be read.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <returns>The number of bytes actually read.</returns>
        public int Read(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Reads data on the socket, returns the number of bytes read.
        /// </summary>
        /// <param name="buffer">The buffer into which the data should be stored.</param>
        /// <param name="offset">The offset into the buffer at which the data should be stored.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <list type="bullet">
        /// <item>
        /// <description><paramref name="offset"/> is less than 0 or <paramref name="offset"/> is greater than <paramref name="buffer"/> length.</description>
        /// </item>
        /// <item>
        /// <description><paramref name="count"/> is less than 0 or <paramref name="count"/> is greater than <paramref name="buffer"/> length minus <paramref name="offset"/></description>
        /// </item>
        /// </list>
        /// </exception>
        /// <returns>The number of bytes actually read.</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (_disposedValue) throw new ObjectDisposedException("TcpIpSocket");
            if (offset < 0 || offset >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

            if (HasData || Socket.Available > 0)
            {
                // Do we have valid data in the buffer from the BeginReceive()
                // call?
                if (HasData)
                {
                    // Yes. Copy this data into the destination buffer and continue
                    // to get the rest of the data.
                    HasData = false;
                    Buffer.CopyTo(buffer, offset);
                    if (count == Buffer.Length)
                    {
                        return count;
                    }
                    else
                    {
                        return Stream.Read(buffer, offset + Buffer.Length, count - Buffer.Length) + Buffer.Length;
                    }
                }
                else
                {
                    // Simply read the data from the socket.
                    return Stream.Read(buffer, offset, count);
                }
            }
            return 0;
        }

        /// <summary>
        /// Writes <paramref name="buffer"/> length bytes to the socket .
        /// </summary>
        /// <param name="buffer">The buffer with the data that should be written.</param>
        /// <returns>The number of bytes actually written.</returns>
        public int Write(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Sends data to the remote host.
        /// </summary>
        /// <param name="buffer">The buffer with the data that should be written.</param>
        /// <param name="offset">The offset of the data to write.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <list type="bullet">
        /// <item>
        /// <description><paramref name="offset"/> is less than 0 or <paramref name="offset"/> is greater than <paramref name="buffer"/> length.</description>
        /// </item>
        /// <item>
        /// <description><paramref name="count"/> is less than 0 or <paramref name="count"/> is greater than <paramref name="buffer"/> length minus <paramref name="offset"/></description>
        /// </item>
        /// </list>
        /// </exception>
        /// <returns>The number of bytes actually written.</returns>
        public int Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (_disposedValue) throw new ObjectDisposedException("TcpIpSocket");
            if (offset < 0 || offset >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

            if (Stream != null && Socket.Client.Poll(1000, SelectMode.SelectWrite))
            {
                Stream?.Write(buffer, offset, count);
                return count;
            }
            return 0;
        }
        #endregion

        #region Internal methods.
        /// <summary>
        /// Closes the socket and disposes of the instance.
        /// </summary>
        internal void Close()
        {
            if (Socket != null)
            {
                Dispose();
            }
        }
        #endregion

        #region IDisposable Support
        bool _disposedValue;

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (Stream != null)
                    {
                        Stream.Close();
                        Stream.Dispose();
                    }
                    if (Socket != null) Socket.Close();
                    Socket = null;
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
