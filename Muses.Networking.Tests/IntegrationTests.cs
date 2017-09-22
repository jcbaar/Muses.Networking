using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Muses.Networking.Tests
{
    [TestClass]
    [TestCategory("Integration tests")]
    public class IntegrationTests
    {
        [TestMethod]
        public void TcpClient_Connects_TcpIpServer_CallsOnConnected_OnBoth()
        {
            using (var sprovider = new TestServiceProvider(true))
            {
                using (var cprovider = new TestServiceProvider())
                {
                    using (var server = new TestTcpIpServer(sprovider))
                    {
                        server.Server.Start();

                        using (var client = new TcpIpClient(cprovider))
                        {
                            client.Connect("127.0.0.1", server.Port);

                            sprovider.ConnectedEvent.WaitOne(1000);
                            cprovider.ConnectedEvent.WaitOne(1000);

                            Assert.IsTrue(sprovider.ConnectedCalled, "TcpIpServer provider OnConnected not called.");
                            Assert.IsTrue(cprovider.ConnectedCalled, "TcpIpClient provider OnConnected not called.");
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TcpIpClient_Sends_TcpIpServer_Echoes_Ok()
        {
            using (var sprovider = new TestServiceProvider(true))
            {
                using (var cprovider = new TestServiceProvider())
                {
                    using (var server = new TestTcpIpServer(sprovider))
                    {
                        server.Server.Start();

                        using (var client = new TcpIpClient(cprovider))
                        {
                            client.Connect("127.0.0.1", server.Port);

                            // Write to server.
                            client.Socket.Write(Encoding.ASCII.GetBytes("Hello World!"));
                            sprovider.ReceiveEvent.WaitOne(1000);
                            Assert.IsTrue(sprovider.ReceiveDataCalled, "TcpIpServer provider OnReceivedData not called.");
                            Assert.AreEqual("Hello World!", sprovider.ReceivedData, "TcpIpServer provider received mismatched data.");

                            // Server should have echoed the message back.
                            cprovider.ReceiveEvent.WaitOne(1000);
                            Assert.IsTrue(cprovider.ReceiveDataCalled, "TcpIpClient provider OnReceivedData not called.");
                            Assert.AreEqual("Hello World!", cprovider.ReceivedData, "TcpIpClient provider received mismatched data.");
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TcpIpServer_Broadcast_Reaches_TcpIpClient_Ok()
        {
            using (var sprovider = new TestServiceProvider(true))
            {
                using (var cprovider = new TestServiceProvider())
                {
                    using (var server = new TestTcpIpServer(sprovider))
                    {
                        server.Server.Start();

                        using (var client = new TcpIpClient(cprovider))
                        {
                            client.Connect("127.0.0.1", server.Port);

                            cprovider.ConnectedEvent.WaitOne(1000);

                            // Broadcast message.
                            server.Server.BroadcastMessage(Encoding.ASCII.GetBytes("Hello World!"));
                            cprovider.ReceiveEvent.WaitOne(1000);
                            Assert.IsTrue(cprovider.ReceiveDataCalled, "TcpIpClient provider OnReceivedData not called.");
                            Assert.AreEqual("Hello World!", cprovider.ReceivedData, "TcpIpClient provider received mismatched data.");
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TcpIpClient_Connect_TcpIpServer_IsConnected_Ok()
        {
            using (var sprovider = new TestServiceProvider(true))
            {
                using (var cprovider = new TestServiceProvider())
                {
                    using (var server = new TestTcpIpServer(sprovider))
                    {
                        server.Server.Start();

                        using (var client = new TcpIpClient(cprovider))
                        {
                            client.Connect("127.0.0.1", server.Port);

                            sprovider.ConnectedEvent.WaitOne(1000);

                            Assert.IsTrue(client.Socket.IsConnected, "TcpClient not connected after connect.");
                            Assert.AreEqual("127.0.0.1", client.Socket.RemoteEndPoint.Address.ToString());
                        }
                    }
                }
            }
        }

        // MaxConnections
        [TestMethod]
        public void TcpIpClient_Connect_TcpIpServer_MaxConnections_Ok()
        {
            using (var sprovider = new TestServiceProvider(true))
            {
                using (var cprovider = new TestServiceProvider())
                {
                    using (var server = new TestTcpIpServer(sprovider))
                    {
                        server.Server.MaxConnections = 1;
                        server.Server.Start();

                        using (var client = new TcpIpClient(cprovider))
                        {
                            using (var client2 = new TcpIpClient(cprovider))
                            {
                                client.Connect("127.0.0.1", server.Port);

                                sprovider.ConnectedEvent.WaitOne(1000);

                                Assert.IsTrue(client.Socket.IsConnected, "TcpClient not connected after connect.");

                                client2.Connect("127.0.0.1", server.Port);

                                sprovider.MaxConnmectionsEvent.WaitOne();

                                Assert.IsTrue(sprovider.MaxConnectionsCalled, "TcpServer MaxConnectionsReched not called");
                            }                            
                        }
                    }
                }
            }
        }
    }
}

