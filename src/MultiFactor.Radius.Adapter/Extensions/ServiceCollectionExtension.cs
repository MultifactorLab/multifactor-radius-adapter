using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MultiFactor.Radius.Adapter.Infrastructure.Http;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.Processing;
using System;
using System.Net.Http;
using System.Security.Authentication;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;

namespace MultiFactor.Radius.Adapter.Extensions;

public static class ServiceCollectionExtension
{
    public static void AddFirstAuthFactorProcessing(this IServiceCollection services)
    {
        services.AddSingleton<IFirstFactorAuthenticationProcessor, LdapFirstFactorAuthenticationProcessor>();
        services.AddSingleton<IFirstFactorAuthenticationProcessor, RadiusFirstFactorAuthenticationProcessor>();
        services.AddSingleton<IFirstFactorAuthenticationProcessorProvider, FirstFactorAuthenticationProcessorProvider>();
    }

    public static void AddHttpServices(this IServiceCollection services)
    {
        services.TryAddSingleton<HttpClientAdapter>();

        services.AddHttpClient(nameof(HttpClientAdapter), (prov, client) =>
        {
            var config = prov.GetRequiredService<IServiceConfiguration>();
            client.BaseAddress = new Uri(config.ApiUrl);
            client.Timeout = config.ApiTimeout;
        }).ConfigurePrimaryHttpMessageHandler(prov =>
        {
            var config = prov.GetRequiredService<IServiceConfiguration>();
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = 100,
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
            };

            if (string.IsNullOrWhiteSpace(config.ApiProxy))
            {
                return handler;
            }

            if (!WebProxyFactory.TryCreateWebProxy(config.ApiProxy, out var webProxy))
            {
                throw new Exception(
                    "Unable to initialize WebProxy. Please, check whether multifactor-api-proxy URI is valid.");
            }

            handler.Proxy = webProxy;

            return handler;
        });
    }
}
