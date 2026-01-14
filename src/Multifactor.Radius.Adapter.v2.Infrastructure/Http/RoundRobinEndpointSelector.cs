using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Http;

public interface IEndpointSelector
{
    Task<string> GetNextEndpointAsync();
}

public class RoundRobinEndpointSelector : IEndpointSelector
{
    private readonly List<string> _endpoints;
    private readonly ConcurrentDictionary<string, bool> _failedEndpoints;
    private int _currentIndex = -1;
    private readonly object _lock = new object();
    private readonly ILogger<RoundRobinEndpointSelector> _logger;

    public RoundRobinEndpointSelector(IConfiguration configuration, 
        ILogger<RoundRobinEndpointSelector> logger)
    {
        _endpoints = configuration.GetSection("ApiEndpoints")
            .Get<List<string>>() ?? [];
        _failedEndpoints = new ConcurrentDictionary<string, bool>();
        _logger = logger;
    }

    public async Task<string> GetNextEndpointAsync()
    {
        return await GetNextHealthyEndpointAsync();
    }

    private Task<string> GetNextHealthyEndpointAsync()
    {
        if (_endpoints.Count == 0)
            throw new InvalidOperationException("No endpoints configured");

        lock (_lock)
        {
            foreach (var t in _endpoints)
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