using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Multifactor
{
    internal class DynamicProxyHandler : HttpMessageHandler
    {
        private readonly IProxySelector _proxySelector;
        private HttpMessageInvoker _currentInvoker;
        private readonly object _lock = new object();

        public DynamicProxyHandler(IProxySelector proxySelector)
        {
            _proxySelector = proxySelector;
            CreateNewHandler();
        }

        private void CreateNewHandler()
        {
            var handler = new SocketsHttpHandler
            {
                MaxConnectionsPerServer = 100,
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
                CreateNewHandler();
                Task.Run(() => oldInvoker?.Dispose());
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpMessageInvoker invokerToUse;
            lock (_lock)
            {
                invokerToUse = _currentInvoker;
            }

            // HttpMessageInvoker имеет публичный метод SendAsync
            return await invokerToUse.SendAsync(request, cancellationToken);
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

}
