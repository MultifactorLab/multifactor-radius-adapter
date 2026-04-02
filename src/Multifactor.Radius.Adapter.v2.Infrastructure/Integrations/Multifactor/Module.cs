using System.Diagnostics;
using System.Security.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using OpenTelemetry.Trace;
using Polly;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Multifactor;

public static class Module
{
    private const string MfTraceIdKey = "mf-trace-id";
    private const string MfTraceIdTag = "mf.trace_id";

    public static void AddMultifactorApi(this IServiceCollection services)
    {
        services.AddTelemetry();
        services.AddSingleton<IEndpointSelector, RoundRobinEndpointSelector>();
        services.AddHttpClient("multifactor-api")
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<ServiceConfiguration>();
                if (config.RootConfiguration.MultifactorApiUrls.Any())
                {
                    var primaryUrl = config.RootConfiguration.MultifactorApiUrls[0];
                    client.BaseAddress = primaryUrl;
                    client.Timeout = config.RootConfiguration.MultifactorApiTimeout;
                }
            })
            .AddPolicyHandler((serviceProvider, request) => {

                var config = serviceProvider.GetRequiredService<ServiceConfiguration>();
                var timeout = config.RootConfiguration.MultifactorApiTimeout;
                var selector = serviceProvider.GetRequiredService<IEndpointSelector>();

                return Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .OrResult(response => !response.IsSuccessStatusCode && (int)response.StatusCode >= 500)
                    .RetryAsync(
                        retryCount: config.RootConfiguration.MultifactorApiUrls.Count - 1,
                        onRetryAsync: async (outcome, retryNumber, context) =>
                        {
                            var fallbackUrl = await selector.GetNextEndpointAsync();
                            request.RequestUri = new Uri(fallbackUrl, request.RequestUri!.PathAndQuery);
                        })
                    .WrapAsync(Policy.TimeoutAsync<HttpResponseMessage>(timeout));
            } 
        )
        .ConfigurePrimaryHttpMessageHandler(provider =>
        {
            var config = provider.GetRequiredService<ServiceConfiguration>();
            var handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = 100,
                SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
            };
            
            if (string.IsNullOrWhiteSpace(config.RootConfiguration.MultifactorApiProxy))
                return handler;
            
            if (!WebProxyFactory.TryCreateWebProxy(config.RootConfiguration.MultifactorApiProxy, out var webProxy))
                throw new Exception(
                    "Unable to initialize WebProxy. Please, check whether multifactor-api-proxy URI is valid.");
            
            handler.Proxy = webProxy;
            return handler;
        });
    }

    private static void AddTelemetry(this IServiceCollection services)
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
        
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            if (!request.Headers.Contains(MfTraceIdKey))
                            {
                                request.Headers.TryAddWithoutValidation(MfTraceIdKey, activity.GetOrCreateMfTraceId());
                            }
                        }; 
                        options.EnrichWithHttpResponseMessage = (activity, response) =>
                        {
                            if (!response.Headers.Contains(MfTraceIdKey))
                            {
                                response.Headers.TryAddWithoutValidation(MfTraceIdKey, activity.GetOrCreateMfTraceId());
                            }
                        };
                    })
                    .AddSource("Multifactor.Radius.Adapter.v2.*");
            });
    }

    private static string GetOrCreateMfTraceId(this Activity? activity)
    {
        return activity?.GetTagItem(MfTraceIdTag)?.ToString()
               ?? activity?.GetBaggageItem(MfTraceIdTag)
               ?? Guid.NewGuid().ToString();
    }
}