using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Radius.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Client;

public sealed class RadiusClientFactory : IRadiusClientFactory
{
    private readonly ILogger<RadiusClient> _logger;
    
    public RadiusClientFactory(ILogger<RadiusClient> logger)
    {
        _logger = logger;
    }

    public IRadiusClient CreateRadiusClient(IPEndPoint localEndpoint)
    {
        ArgumentNullException.ThrowIfNull(localEndpoint);
        
        return new RadiusClient(localEndpoint, _logger);
    }
}