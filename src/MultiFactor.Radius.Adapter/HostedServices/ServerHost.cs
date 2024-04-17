using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.HostedServices
{
    public class ServerHost : IHostedService
    {
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new();

        private readonly ILogger _logger;
        private readonly RadiusServer _radiusServer;

        public ServerHost(ILogger<ServerHost> logger, RadiusServer radiusServer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _radiusServer = radiusServer ?? throw new ArgumentNullException(nameof(radiusServer));
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _radiusServer.Start();

            // Store the task we're executing
            _executingTask = ExecuteAsync(_stoppingCts.Token);

            // If the task is completed then return it,
            // this will bubble cancellation and failure to the caller
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                _radiusServer.Stop();
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StopAsync");
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }

        protected async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                //infinite job
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}
