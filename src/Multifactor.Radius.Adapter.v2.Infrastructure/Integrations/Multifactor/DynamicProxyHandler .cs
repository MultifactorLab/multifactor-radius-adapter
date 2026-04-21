using System.Net.Security;
using System.Security.Authentication;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Multifactor;

internal class DynamicProxyHandler : HttpMessageHandler
{
    private readonly IProxySelector _proxySelector;
    private HttpMessageInvoker _currentInvoker;
    private readonly object _lock = new object();
    private TimeSpan _proxyTimeout = TimeSpan.FromSeconds(30);

    public DynamicProxyHandler(IProxySelector proxySelector)
    {
        _proxySelector = proxySelector;
        CreateHandler();
    }

    public void SetProxyTimeout(TimeSpan timeout)
    {
        _proxyTimeout = timeout;
        UpdateProxy();
    }

    private void CreateHandler()
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 100,
            ConnectTimeout = _proxyTimeout,
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
            }
        };

        var proxy = _proxySelector.GetCurrentProxy();
        if (proxy != null && WebProxyFactory.TryCreateWebProxy(proxy, out var webProxy))
        {
            handler.Proxy = webProxy;
            handler.UseProxy = true;
        }

        _currentInvoker = new HttpMessageInvoker(handler);
    }

    public void UpdateProxy()
    {
        lock (_lock)
        {
            var oldInvoker = _currentInvoker;
            CreateHandler();
            Task.Run(() => oldInvoker?.Dispose());
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpMessageInvoker invoker;
        lock (_lock)
        {
            invoker = _currentInvoker;
        }
        
        return await invoker.SendAsync(request, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_lock)
            {
                _currentInvoker?.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}