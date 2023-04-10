using Microsoft.Extensions.Hosting;
using MultiFactor.Radius.Adapter.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.HostedServices
{
    public class Starter : IHostedService
    {
        private readonly ServerInfo _serverInfo;

        public Starter(ServerInfo serverInfo)
        {
            _serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _serverInfo.Initialize();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
