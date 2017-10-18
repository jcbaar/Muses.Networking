using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Sockets;

namespace Muses.Networking.Tests
{
    [TestClass]
    [TestCategory("TcpIpSocket")]
    public class TcpIpSocketTests
    {
        [TestMethod]
        public void TcpIpSocket_Construction_Null_Socket_Argument_Throws()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new TcpIpSocket(null, null));
        }

        [TestMethod]
        public void TcpIpSocket_Construction_Null_Provider_Argument_Throws()
        {
            using (var socket = new TcpClient())
            {
                Assert.ThrowsException<ArgumentNullException>(() => new TcpIpSocket(socket, null));
            }
        }

        [TestMethod]
        public void TcpIpSocket_ReadWrite_NullBuffer_Throws()
        {
            using (var socket = new TcpClient())
            {
                using (var provider = new TestServiceProvider())
                {
                    using (var tcpsock = new TcpIpSocket(socket, provider))
                    {
                        Assert.ThrowsException<ArgumentNullException>(() => tcpsock.Write(null));
                        Assert.ThrowsException<ArgumentNullException>(() => tcpsock.Read(null));
                        Assert.ThrowsException<ArgumentNullException>(() => tcpsock.Write(null, 0, 1));
                        Assert.ThrowsException<ArgumentNullException>(() => tcpsock.Read(null, 0, 1));
                    }
                }
            }
        }

        [TestMethod]
        public void TcpIpSocket_ReadWrite_ArgumentsOutOfRange_Throws()
        {
            using (var socket = new TcpClient())
            {
                using (var provider = new TestServiceProvider())
                {
                    using (var tcpsock = new TcpIpSocket(socket, provider))
                    {
                        var b = new byte[10];
                        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tcpsock.Write(b, -1, 1));
                        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tcpsock.Write(b, 10, 1));
                        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tcpsock.Write(b, 0, -1));
                        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tcpsock.Write(b, 0, 11));
                        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tcpsock.Write(b, 5, 6));
                        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tcpsock.Read(b, -1, 1));
                        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tcpsock.Read(b, 10, 1));
                        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tcpsock.Read(b, 0, -1));
                        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tcpsock.Read(b, 0, 11));
                        Assert.ThrowsException<ArgumentOutOfRangeException>(() => tcpsock.Read(b, 5, 6));
                    }
                }
            }
        }

        [TestMethod]
        public void TcpIpSocket_Read_Closed_Throws()
        {
            using (var socket = new TcpClient())
            {
                using (var provider = new TestServiceProvider())
                {
                    var tcpsock = new TcpIpSocket(socket, provider);

                    tcpsock.Close();

                    Assert.ThrowsException<ObjectDisposedException>(() => tcpsock.Read(new byte[1]));
                }
            }
        }

        [TestMethod]
        public void TcpIpSocket_Write_Closed_Throws()
        {
            using (var socket = new TcpClient())
            {
                using (var provider = new TestServiceProvider())
                {
                    var tcpsock = new TcpIpSocket(socket, provider);

                    tcpsock.Close();

                    Assert.ThrowsException<ObjectDisposedException>(() => tcpsock.Write(new byte[1]));
                }
            }
        }
    }
}
