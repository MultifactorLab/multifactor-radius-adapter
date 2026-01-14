using System.Security.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Cache;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Services;
using Multifactor.Radius.Adapter.v2.Application.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Ldap;
using Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor;
using Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Udp;
using Multifactor.Radius.Adapter.v2.Infrastructure.Cache;
using Multifactor.Radius.Adapter.v2.Infrastructure.Cache.AuthenticatedClientCache;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Loader;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser.ValueParser;
using Multifactor.Radius.Adapter.v2.Infrastructure.Http;
using Multifactor.Radius.Adapter.v2.Infrastructure.Logging;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Client;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Services;
using Multifactor.Radius.Adapter.v2.Shared;
using Polly;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static void AddConfiguration(this IServiceCollection services)
    {        
        services.AddSingleton<RadiusDictionary>();
        services.AddSingleton<IRadiusDictionary>(prov =>
        {
            var dict = prov.GetRequiredService<RadiusDictionary>();
            dict.Read();
            return dict;
        });
        
        services.AddSingleton<IValueParser, ValueParser>();
        services.AddSingleton<IConfigurationParser, XmlConfigurationParser>();
        services.AddSingleton<IConfigurationLoader, ConfigurationLoader>();

        services.AddSingleton<ServiceConfiguration>(provider =>
        {
            var manager = provider.GetRequiredService<IConfigurationLoader>();
            return manager.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        });
    }
    
    public static IServiceCollection AddRadiusUdpClient(this IServiceCollection services)
    {
        services.AddSingleton<IUdpClient>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<ServiceConfiguration>();
            var endpoint = config.RootConfiguration.AdapterServerEndpoint;
            var logger = serviceProvider.GetService<ILogger<CustomUdpClient>>();
            var options = serviceProvider.GetService<IOptions<UdpClientOptions>>();
        
            return new CustomUdpClient(endpoint, logger, options);
        });
    
        return services;
    }
    

    public static void AddMultifactorApi(this IServiceCollection services)
    {
        services.AddSingleton<IEndpointSelector, RoundRobinEndpointSelector>();
        services.AddHttpClient("multifactor-api")
        .AddPolicyHandler((serviceProvider, request) =>
            Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(response => !response.IsSuccessStatusCode)
                .FallbackAsync(
                    fallbackAction: async (outcome, context, cancellationToken) =>
                    {
                        var urlSelector = serviceProvider.GetRequiredService<IEndpointSelector>();
                        var fallbackUrl = await urlSelector.GetNextEndpointAsync();
                    
                        var fallbackRequest = request.CloneHttpRequestMessage();
                        fallbackRequest.RequestUri = new Uri(new Uri(fallbackUrl), request.RequestUri!.PathAndQuery);
                    
                        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                        var httpClient = httpClientFactory.CreateClient("FallbackClient");
                    
                        return await httpClient.SendAsync(fallbackRequest, cancellationToken);
                    },
                    onFallbackAsync: (outcome, context) =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger>();
                        logger.LogWarning("Primary endpoint failed. Trying fallback. Error: {Error}", 
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                        return Task.CompletedTask;
                    })
                .WrapAsync(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)))
        )
        .AddHttpMessageHandler<MfTraceIdHeaderSetter>()
        .ConfigurePrimaryHttpMessageHandler(provider =>
        {
            var config = provider.GetRequiredService<ServiceConfiguration>();
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = 100,
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
            };
            
            if (config.RootConfiguration.MultifactorApiProxy == null)
                return handler;
            
            if (!WebProxyFactory.TryCreateWebProxy(config.RootConfiguration.MultifactorApiProxy, out var webProxy))
                throw new Exception(
                    "Unable to initialize WebProxy. Please, check whether multifactor-api-proxy URI is valid.");
            
            handler.Proxy = webProxy;

            return handler;
        });

        services.AddSingleton<IMultifactorApi, MultifactorApi>();
    }
    
    public static void AddAdapterLogging(this IServiceCollection services)
    {
        services.AddSerilog((provider, loggerConfiguration) =>
        {
            var serviceConfiguration = provider.GetRequiredService<ServiceConfiguration>();
            var logger = SerilogLoggerFactory.CreateLogger(serviceConfiguration.RootConfiguration);
            Log.Logger = logger;
        });
    }

    public static void AddLdap(this IServiceCollection services)
    {
        services.AddSingleton<ILdapConnectionFactory, CustomLdapConnectionFactory>();
        services.AddSingleton<ILdapGroupLoaderFactory, LdapGroupLoaderFactory>();
        services.AddSingleton<IMembershipCheckerFactory, MembershipCheckerFactory>();
        services.AddSingleton<LdapSchemaLoader>();
        services.AddTransient<ILdapAdapter, LdapAdapter>();
    }

    public static void AddInfraServices(this IServiceCollection services)
    {
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IAuthenticatedClientCache, AuthenticatedClientCache>();
        services.AddTransient<IRadiusPacketService, RadiusPacketService>();
        services.AddSingleton<IRadiusClientFactory, RadiusClientFactory>();
        services.AddTransient<IRadiusReplyAttributeService, RadiusReplyAttributeService>();
        services.AddTransient<IRadiusAttributeTypeConverter, RadiusAttributeTypeConverter>();
    }
    
    public static void AddPipelines(this IServiceCollection services)
    {
        services.AddSingleton<IPipelineProvider, RadiusPipelineProvider>();
        services.AddSingleton<IRadiusPipelineFactory, RadiusPipelineFactory>();
    }
}