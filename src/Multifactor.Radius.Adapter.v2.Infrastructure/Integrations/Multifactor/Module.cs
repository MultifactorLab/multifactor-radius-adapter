using System.Diagnostics;
using System.Net;
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
        services.AddSingleton<IProxySelector, RoundRobinProxySelector>();
        services.AddSingleton<DynamicProxyHandler>();
        services.AddHttpClient("multifactor-api")
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<ServiceConfiguration>();
                if (config.RootConfiguration.MultifactorApiUrls.Any())
                {
                    var selector = serviceProvider.GetRequiredService<IEndpointSelector>();
                    var primaryUrl = selector.GetCurrentEndpoint();
                    client.BaseAddress = primaryUrl;
                    client.Timeout = config.RootConfiguration.MultifactorApiTimeout;
                }
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var config = serviceProvider.GetRequiredService<ServiceConfiguration>();
                var timeout = config.RootConfiguration.MultifactorApiTimeout;
                var endpointSelector = serviceProvider.GetRequiredService<IEndpointSelector>();
                var proxySelector = serviceProvider.GetRequiredService<IProxySelector>();
                var dynamicHandler = serviceProvider.GetRequiredService<DynamicProxyHandler>();

                return Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .OrResult(response => !response.IsSuccessStatusCode && (int)response.StatusCode >= 500)
                    .RetryAsync(
                        retryCount: config.RootConfiguration.MultifactorApiUrls.Count *
                                   (config.RootConfiguration.MultifactorApiProxy?.Count ?? 1) - 1,
                        onRetryAsync: async (outcome, retryNumber, context) =>
                        {
                            var (nextUrl, nextProxy) = await GetNextEndpointAndProxyAsync(
                                endpointSelector, proxySelector);
                            request.RequestUri = new Uri(nextUrl, request.RequestUri!.PathAndQuery);

                            dynamicHandler.UpdateProxy();
                        })
                    .WrapAsync(Policy.TimeoutAsync<HttpResponseMessage>(timeout));
            })
            .ConfigurePrimaryHttpMessageHandler(provider =>
                provider.GetRequiredService<DynamicProxyHandler>());
    }

    private static async Task<(Uri url, WebProxy? proxy)> GetNextEndpointAndProxyAsync(
        IEndpointSelector endpointSelector, 
        IProxySelector proxySelector)
    {
        var nextUrl = await endpointSelector.GetNextEndpointAsync();
        var nextProxyUrl = await proxySelector.GetCurrentProxyAsync();
        if (nextUrl == null && endpointSelector.IsCycleComplete)
        {
            nextProxyUrl = await proxySelector.GetNextProxyAsync();
            endpointSelector.Reset();
            nextUrl = endpointSelector.GetCurrentEndpoint();
        }
    
        WebProxy? webProxy = null;
        if (nextProxyUrl is not null)
        {
            if (!WebProxyFactory.TryCreateWebProxy(nextProxyUrl, out webProxy))
                throw new Exception($"Unable to initialize WebProxy for {nextProxyUrl}");
        }
    
        return (nextUrl, webProxy);
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