using System;

namespace MultiFactor.Radius.Adapter.Core
{
    public class ServerInfo
    {
        private bool _initialized;
        private DateTime _startedAt;

        public void Initialize()
        {
            if (_initialized) throw new InvalidOperationException("Server info can only be initialized once");
            _startedAt = DateTime.Now;
            _initialized = true;
        }

        public TimeSpan GetUptime() => DateTime.Now - _startedAt;
    }
}
