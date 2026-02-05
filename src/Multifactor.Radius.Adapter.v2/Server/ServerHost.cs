using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Multifactor.Radius.Adapter.v2.Server;

public class ServerHost : IHostedService
{
    private readonly AdapterServer _server;
    private readonly ILogger<ServerHost> _logger;
    private Task? _serverTask;
    private CancellationTokenSource? _cts;
    private const int ShoutDownTimeout = 30;
    
    public ServerHost(AdapterServer server, ILogger<ServerHost> logger)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting RADIUS server host...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        try
        {
            _serverTask = _server.StartAsync(_cts.Token);
            await Task.Yield();
            _logger.LogInformation("RADIUS server host started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RADIUS server host");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping RADIUS server host...");
        try
        {
            await _cts?.CancelAsync();
            
            if (_serverTask is { IsCompleted: false })
            {
                await Task.WhenAny(_serverTask, 
                    Task.Delay(TimeSpan.FromSeconds(ShoutDownTimeout), cancellationToken));
            }
            
            _logger.LogInformation("RADIUS server host stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RADIUS server host shutdown");
            throw;
        }
        finally
        {
            _cts?.Dispose();
        }
    }
}