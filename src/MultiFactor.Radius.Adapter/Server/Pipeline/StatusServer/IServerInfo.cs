using System;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.StatusServer;

public interface IServerInfo
{
    TimeSpan GetUptime();
    string GetVersion();
    void Initialize();
}