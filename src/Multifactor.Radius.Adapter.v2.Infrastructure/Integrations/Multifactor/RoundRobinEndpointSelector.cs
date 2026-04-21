using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Multifactor;

internal interface IEndpointSelector
{
    Task<Uri?> GetNextEndpointAsync();
    Uri GetCurrentEndpoint();
    bool IsCycleComplete { get; }
    void Reset();
}

internal sealed class RoundRobinEndpointSelector : IEndpointSelector
{
    private readonly IReadOnlyList<Uri> _endpoints;
    private int _currentIndex = 0;
    private readonly object _lock = new();
    private readonly ILogger<RoundRobinEndpointSelector> _logger;

    public RoundRobinEndpointSelector(
        ServiceConfiguration configuration, 
        ILogger<RoundRobinEndpointSelector> logger)
    {
        _endpoints = configuration.RootConfiguration.MultifactorApiUrls;
        _logger = logger;
    }

    public Task<Uri?> GetNextEndpointAsync()
    {
        return Task.FromResult(GetNextHealthyEndpoint());
    }
    
    public Uri GetCurrentEndpoint()
    {
        return _endpoints[_currentIndex];
    }

    public bool IsCycleComplete => _currentIndex >= _endpoints.Count;
    public void Reset()
    {
        _currentIndex = 0;
    }

    private Uri? GetNextHealthyEndpoint()
    {
        if (_endpoints.Count == 0)
            throw new InvalidOperationException("No endpoints configured");

        lock (_lock)
        {
            _currentIndex = _currentIndex + 1;
            if (IsCycleComplete) return null;
            var endpoint = _endpoints[_currentIndex];
            _logger.LogDebug("Selected endpoint: {Endpoint}", endpoint);
            return endpoint;
        }
    }

}