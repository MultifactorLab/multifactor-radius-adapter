using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;

public class RadiusPipelineProvider : IPipelineProvider
{
    private readonly IRadiusPipelineFactory _pipelineFactory;
    private readonly ILogger<RadiusPipelineProvider> _logger;
    private readonly ConcurrentDictionary<string, IRadiusPipeline> _pipelineCache = new();
    
    public RadiusPipelineProvider(
        IRadiusPipelineFactory pipelineFactory,
        ILogger<RadiusPipelineProvider> logger)
    {
        _pipelineFactory = pipelineFactory;
        _logger = logger;
    }
    
    public IRadiusPipeline GetPipeline(ClientConfiguration clientConfiguration)
    {
        var clientName = clientConfiguration.Name;
        return _pipelineCache.GetOrAdd(clientName, name =>
        {
            _logger.LogDebug("Creating new pipeline for client '{Client}'", name);
            return _pipelineFactory.CreatePipeline(clientConfiguration);
        });
    }
    
}
