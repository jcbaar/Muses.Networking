using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Muses.Networking.Tests
{
    [TestClass]
    [TestCategory("TcpIpClient")]
    public class TcpIpClientTests
    {
        [TestMethod]
        public void TcpIpClient_Construction_NullProvider_Throws()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new TcpIpClient(null));
        }

        [TestMethod]
        public void TcpIpClient_Disconnect_Disconnected_Throws()
        {
            using (var provider = new TestServiceProvider())
            {
                using (var client = new TcpIpClient(provider))
                {
                    Assert.ThrowsException<InvalidOperationException>(() => client.Disconnect());
                }
            }
        }

        [TestMethod]
        public void TcpIpClient_Connect_Connected_Throws()
        {
            using (var provider = new TestServiceProvider())
            {
                using (var server = new TestTcpIpServer(provider))
                {
                    server.Server.Start();

                    using (var client = new TcpIpClient(provider))
                    {
                        client.Connect("127.0.0.1", server.Port);

                        Assert.ThrowsException<InvalidOperationException>(() => client.Connect("127.0.0.1", server.Port));
                    }
                }
            }
        }
    }
}
