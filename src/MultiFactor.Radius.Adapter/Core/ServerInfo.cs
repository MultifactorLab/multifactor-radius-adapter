using System;
using System.Reflection;

namespace MultiFactor.Radius.Adapter.Core
{
    public class ServerInfo : IServerInfo
    {
        private bool _initialized;

        private DateTime _startedAt;
        private string _version;

        public void Initialize()
        {
            if (_initialized) throw new InvalidOperationException("Server info can only be initialized once");

            _startedAt = DateTime.Now;
            _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            _initialized = true;
        }

        public TimeSpan GetUptime()
        {
            if (!_initialized) throw new InvalidOperationException("Server info should be initialized before");
            return DateTime.Now - _startedAt;
        }

        public string GetVersion()
        {
            if (!_initialized) throw new InvalidOperationException("Server info should be initialized before");
            return _version;
        }
    }
}
