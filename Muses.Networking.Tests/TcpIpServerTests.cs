using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace Muses.Networking.Tests
{
    [TestClass]
    [TestCategory("TcpIpServer")]
    public class TcpIpServerTests
    {
        [TestMethod]
        public void TcpIpServer_Construction_IllegalPort_Throws()
        {
            using (var provider = new TestServiceProvider())
            {
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => new TcpIpServer(provider, IPEndPoint.MinPort - 1));
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => new TcpIpServer(provider, IPEndPoint.MaxPort + 1));
            }
        }

        [TestMethod]
        public void TcpIpServer_Construction_NullProvider_Throws()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new TcpIpServer(null, 12345));
        }

        [TestMethod]
        public void TcpIpServer_Construction_Configuration_Ok()
        {
            using (var provider = new TestServiceProvider())
            {
                using (var server = new TestTcpIpServer(provider))
                {
                    Assert.AreEqual(0, server.Server.MaxConnections);
                    Assert.AreEqual(0, server.Server.CurrentConnections);
                }
            }
        }

        [TestMethod]
        public void TcpIpServer_Start_Ok()
        {
            using (var provider = new TestServiceProvider())
            {
                using (var server = new TestTcpIpServer(provider))
                {
                    Assert.IsTrue(server.Server.Start());
                    Assert.IsFalse(server.Server.Start());
                }
            }
        }

        [TestMethod]
        public void TcpIpServer_Start_Stop_Ok()
        {
            using (var provider = new TestServiceProvider())
            {
                using (var server = new TestTcpIpServer(provider))
                {
                    Assert.IsTrue(server.Server.Start());
                    Assert.IsTrue(server.Server.Stop());
                    Assert.IsFalse(server.Server.Stop());
                }
            }
        }
    }
}
