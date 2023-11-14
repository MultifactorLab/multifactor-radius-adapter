using System;

namespace MultiFactor.Radius.Adapter.Core
{
    public interface IServerInfo
    {
        TimeSpan GetUptime();
        string GetVersion();
        void Initialize();
    }
}