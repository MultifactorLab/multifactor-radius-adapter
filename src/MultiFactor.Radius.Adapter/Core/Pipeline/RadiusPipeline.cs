using System.Threading.Tasks;
using System;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Server.Context;
using MultiFactor.Radius.Adapter.Server.Pipeline;

namespace MultiFactor.Radius.Adapter.Core.Pipeline;

public class RadiusPipeline : IRadiusPipeline
{
    private readonly RadiusRequestDelegate _requestDelegate;
    private readonly IRadiusRequestPostProcessor _postProcessor;
    private readonly ILogger<RadiusPipeline> _logger;

    public RadiusPipeline(RadiusRequestDelegate requestDelegate, IRadiusRequestPostProcessor postProcessor, ILogger<RadiusPipeline> logger)
    {
        _requestDelegate = requestDelegate;
        _postProcessor = postProcessor;
        _logger = logger;
    }

    public async Task InvokeAsync(RadiusContext context)
    {
        try
        {
            await _requestDelegate(context);
            await _postProcessor.InvokeAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HandleRequest");
        }
    }
}
