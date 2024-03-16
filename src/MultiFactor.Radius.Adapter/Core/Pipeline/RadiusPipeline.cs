using System.Threading.Tasks;
using System;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Server.Context;
using MultiFactor.Radius.Adapter.Server.Pipeline;
using Microsoft.Extensions.Hosting;

namespace MultiFactor.Radius.Adapter.Core.Pipeline;

public class RadiusPipeline : IRadiusPipeline
{
    private readonly RadiusRequestDelegate _requestDelegate;
    private readonly IRadiusRequestPostProcessor _postProcessor;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<RadiusPipeline> _logger;

    public RadiusPipeline(RadiusRequestDelegate requestDelegate, 
        IRadiusRequestPostProcessor postProcessor, 
        IHostEnvironment environment, 
        ILogger<RadiusPipeline> logger)
    {
        _requestDelegate = requestDelegate;
        _postProcessor = postProcessor;
        _environment = environment;
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
            if (_environment.IsEnvironment("Test"))
            {
                throw new Exception($"Failed to handle radius request: {ex.Message}", ex);
            }
            else
            {
                _logger.LogError(ex, "HandleRequest");
            }
        }
    }
}
