using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class RadiusPipelineProvider : IPipelineProvider
{
    private readonly IRadiusPipelineFactory _pipelineFactory;
    private readonly ILogger<RadiusPipelineProvider> _logger;
    private readonly ConcurrentDictionary<string, IRadiusPipeline> _pipelineCache = new();
    private readonly ServiceConfiguration _serviceConfiguration;
    
    public RadiusPipelineProvider(
        IRadiusPipelineFactory pipelineFactory,
        ILogger<RadiusPipelineProvider> logger,
        ServiceConfiguration serviceConfiguration)
    {
        _pipelineFactory = pipelineFactory;
        _logger = logger;
        _serviceConfiguration = serviceConfiguration;
    }
    
    public IRadiusPipeline GetPipeline(string clientName)
    {
        return _pipelineCache.GetOrAdd(clientName, name =>
        {
            _logger.LogDebug("Creating new pipeline for client '{Client}'", name);
            return _pipelineFactory.CreatePipeline(GetClientConfiguration(name));
        });
    }
    
    private ClientConfiguration GetClientConfiguration(string clientName)
    {
        return _serviceConfiguration.GetClientConfiguration(clientName);
    }
    
    public void ClearCache()
    {
        _pipelineCache.Clear();
        _logger.LogInformation("Pipeline cache cleared");
    }
    
    public void RemoveFromCache(string clientName)
    {
        _pipelineCache.TryRemove(clientName, out _);
        _logger.LogDebug("Removed pipeline for client '{Client}' from cache", clientName);
    }
}
