using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor.Http;

public interface IEndpointSelector
{
    Task<Uri> GetNextEndpointAsync();
}

public class RoundRobinEndpointSelector : IEndpointSelector
{
    private readonly IReadOnlyList<Uri> _endpoints;
    private readonly ConcurrentDictionary<Uri, bool> _failedEndpoints;
    private int _currentIndex = -1;
    private readonly object _lock = new();
    private readonly ILogger<RoundRobinEndpointSelector> _logger;

    public RoundRobinEndpointSelector(ServiceConfiguration configuration, 
        ILogger<RoundRobinEndpointSelector> logger)
    {
        _endpoints = configuration.RootConfiguration.MultifactorApiUrls;
        _failedEndpoints = new ConcurrentDictionary<Uri, bool>();
        _logger = logger;
    }

    public async Task<Uri> GetNextEndpointAsync()
    {
        return await GetNextHealthyEndpointAsync();
    }

    private Task<Uri> GetNextHealthyEndpointAsync()
    {
        if (_endpoints.Count == 0)
            throw new InvalidOperationException("No endpoints configured");

        lock (_lock)
        {
            foreach (var _ in _endpoints)
            {
                _currentIndex = (_currentIndex + 1) % _endpoints.Count;
                var endpoint = _endpoints[_currentIndex];

                if (_failedEndpoints.ContainsKey(endpoint)) continue;
                _logger.LogDebug("Selected endpoint: {Endpoint}", endpoint);
                return Task.FromResult(endpoint);
            }
            _failedEndpoints.Clear();
            _currentIndex = 0;
            _logger.LogWarning("All endpoints failed, resetting to first");
            return Task.FromResult(_endpoints[0]);
        }
    }
}