using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Multifactor;

internal interface IEndpointSelector
{
    Task<Uri> GetNextEndpointAsync();
}

internal sealed class RoundRobinEndpointSelector : IEndpointSelector
{
    private readonly IReadOnlyList<Uri> _endpoints;
    private readonly ConcurrentDictionary<Uri, DateTime> _failedEndpoints;
    private int _currentIndex = -1;
    private readonly object _lock = new();
    private readonly ILogger<RoundRobinEndpointSelector> _logger;
    private readonly TimeSpan _failureRetryPeriod = TimeSpan.FromMinutes(5);

    public RoundRobinEndpointSelector(
        ServiceConfiguration configuration, 
        ILogger<RoundRobinEndpointSelector> logger)
    {
        _endpoints = configuration.RootConfiguration.MultifactorApiUrls;
        _failedEndpoints = new ConcurrentDictionary<Uri, DateTime>();
        _logger = logger;
    }

    public Task<Uri> GetNextEndpointAsync()
    {
        return Task.FromResult(GetNextHealthyEndpoint());
    }

    private Uri GetNextHealthyEndpoint()
    {
        if (_endpoints.Count == 0)
            throw new InvalidOperationException("No endpoints configured");

        CleanupOldFailures();

        lock (_lock)
        {
            for (int i = 0; i < _endpoints.Count; i++)
            {
                _currentIndex = (_currentIndex + 1) % _endpoints.Count;
                var endpoint = _endpoints[_currentIndex];
                if (_failedEndpoints.ContainsKey(endpoint)) continue;
                _logger.LogDebug("Selected endpoint: {Endpoint}", endpoint);
                return endpoint;
            }
            _logger.LogWarning("All endpoints are marked as failed, trying first endpoint");
            return _endpoints[0];
        }
    }

    private void CleanupOldFailures()
    {
        var cutoff = DateTime.UtcNow - _failureRetryPeriod;
        foreach (var kvp in _failedEndpoints)
        {
            if (kvp.Value < cutoff)
            {
                _failedEndpoints.TryRemove(kvp.Key, out _);
                _logger.LogDebug("Removed expired failure for {Endpoint}", kvp.Key);
            }
        }
    }
}