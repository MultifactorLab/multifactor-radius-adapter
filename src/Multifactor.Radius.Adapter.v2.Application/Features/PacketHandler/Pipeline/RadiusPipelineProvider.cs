using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Pipeline;

public interface IPipelineProvider
{
    public IRadiusPipeline GetPipeline(IClientConfiguration clientConfiguration);
}

internal sealed class RadiusPipelineProvider : IPipelineProvider
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
    
    public IRadiusPipeline GetPipeline(IClientConfiguration clientConfiguration)
    {
        var clientName = clientConfiguration.Name;
        return _pipelineCache.GetOrAdd(clientName, name =>
        {
            _logger.LogDebug("Creating new pipeline for client '{Client}'", name);
            return _pipelineFactory.CreatePipeline(clientConfiguration);
        });
    }
}
