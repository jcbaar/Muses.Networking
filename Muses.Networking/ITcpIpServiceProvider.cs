using System;

namespace Muses.Networking
{
    /// <summary>
    /// Service provider for the TcpIpServer and TcpIpClient object. Contains callbacks to handle
    /// server/client events properly.
    /// </summary>
    public interface ITcpIpServiceProvider
    {
        /// <summary>
        /// Gets executed when the <see cref="TcpIpServer"/> accepts a new connection or, in case
        /// of a <see cref="TcpIpClient"/>, the socket is connected. 
        /// </summary>
        /// <param name="socket">The connected socket.</param>
        void OnConnected(TcpIpSocket socket);

        /// <summary>
        /// Gets executed when the <see cref="TcpIpServer"/> or <see cref="TcpIpClient"/>
        /// detects incoming data.
        /// </summary>
        /// <param name="socket">The socket which detected incoming data.</param>
        void OnReceiveData(TcpIpSocket socket);

        /// <summary>
        /// Gets executed when the server/client is about to shutdown the connection.
        /// </summary>
        /// <param name="socket">The socket about to be closed.</param>
        void OnClosingConnection(TcpIpSocket socket);

        /// <summary>
        /// Gets executed when an exception occurs in the <see cref="TcpIpServer"/>
        /// or <see cref="TcpIpClient"/>.
        /// </summary>
        /// <param name="socket">The socket that caused the exception. Can be null!</param>
        /// <param name="ex">The exception.</param>
        void OnException(TcpIpSocket socket, Exception ex);

        /// <summary>
        /// Gets executed when a connection attempt was made whilst the maximum
        /// number of connections has already been reached. This callback is never
        /// executed when the provider is connected to a TcpIpClient object.
        /// </summary>
        /// <param name="socket">The socket that is being refused.</param>
        /// <param name="allowAnyway">Set to true if you want the connection
        /// to be accepted anyway. Is set to false by default.</param>
        void OnMaxConnectionsReached(TcpIpSocket socket, ref bool allowAnyway);
    }
}
