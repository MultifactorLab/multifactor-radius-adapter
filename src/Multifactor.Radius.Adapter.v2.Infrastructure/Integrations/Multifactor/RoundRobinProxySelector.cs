using System.Net;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Multifactor;

internal interface IProxySelector
{
    WebProxy GetNextProxy();
}

internal sealed class RoundRobinProxySelector : IProxySelector
{
    private readonly List<WebProxy?> _proxies;
    private int _currentIndex = -1;
    private readonly object _lock = new();
    private readonly ILogger<RoundRobinProxySelector> _logger;

    public RoundRobinProxySelector(
        ServiceConfiguration configuration,
        ILogger<RoundRobinProxySelector> logger)
    {
        _proxies = configuration.RootConfiguration.MultifactorApiProxies
            .Select(proxyString => !WebProxyFactory.TryCreateWebProxy(proxyString, out var proxy) ? throw new Exception($"Invalid proxy: {proxyString}") : proxy)
            .ToList();
        _logger = logger;
    }

    public WebProxy GetNextProxy()
    {
        if (_proxies.Count == 0)
            return null;

        lock (_lock)
        {
            _currentIndex = (_currentIndex + 1) % _proxies.Count;
            var proxy = _proxies[_currentIndex];
            _logger.LogDebug("Selected proxy: {Proxy}", proxy.Address);
            return proxy;
        }
    }
}