using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Multifactor;

internal interface IProxySelector
{
    Task<Uri?> GetNextProxyAsync();
    Task<Uri?> GetCurrentProxyAsync();
    Uri? GetCurrentProxy();
}

internal sealed class RoundRobinProxySelector : IProxySelector
{
    private readonly IReadOnlyList<Uri>? _proxies;
    private int _currentIndex = 0;
    private readonly object _lock = new();
    private readonly ILogger<RoundRobinEndpointSelector> _logger;

    public RoundRobinProxySelector(
        ServiceConfiguration configuration, 
        ILogger<RoundRobinEndpointSelector> logger)
    {
        _proxies = configuration.RootConfiguration.MultifactorApiProxy;
        _logger = logger;
    }

    public Task<Uri?> GetNextProxyAsync()
    {
        return Task.FromResult(GetNextHealthyProxy());
    }
    public Task<Uri?> GetCurrentProxyAsync()
    {
        return Task.FromResult(GetCurrentProxy());
    }

    public Uri? GetCurrentProxy()
    {
        return _proxies?.Count > 0 ? _proxies[_currentIndex] : null;
    }

    private Uri? GetNextHealthyProxy()
    {
        if (_proxies is null || _proxies?.Count == 0)
            return null;

        lock (_lock)
        {
            _currentIndex = (_currentIndex + 1) % _proxies!.Count;
            var proxy = _proxies[_currentIndex];
            _logger.LogDebug("Selected proxy: {proxy}", proxy);
            return proxy;
        }
    }

}