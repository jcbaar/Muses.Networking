using System;
using System.Collections.Concurrent;

namespace Muses.Networking.Tests
{
    class TestTcpIpServer : IDisposable
    {
        ConcurrentDictionary<int, bool> _cache = new ConcurrentDictionary<int, bool>();

        public TcpIpServer Server { get; set; }
        public int Port { get; set; }

        public TestTcpIpServer(ITcpIpServiceProvider provider)
        {
            var rand = new Random();
            Port = rand.Next(10000, 20000);
            while(_cache.ContainsKey(Port))
            {
                Port = rand.Next(10000, 20000);
            }

            _cache[Port] = true;
            Server = new TcpIpServer(provider, Port);
        }

        #region IDisposable Support
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (Server != null) Server.Dispose();
                    _cache.TryRemove(Port, out bool dummy);
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
