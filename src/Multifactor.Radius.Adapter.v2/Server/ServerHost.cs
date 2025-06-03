using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.LangFeatures;

namespace Multifactor.Radius.Adapter.v2.Server;

public class ServerHost : IHostedService
{
    private readonly AdapterServer _server;
    private readonly ILogger<ServerHost> _logger;
    
    public ServerHost(AdapterServer server, ILogger<ServerHost> logger)
    {
        Throw.IfNull(server, nameof(server));
        Throw.IfNull(logger, nameof(logger));
        _server = server;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
           var task = _server.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _server.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }

        return Task.CompletedTask;
    }
}