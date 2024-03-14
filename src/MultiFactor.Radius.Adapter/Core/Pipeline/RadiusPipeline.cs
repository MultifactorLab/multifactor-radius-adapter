using System.Threading.Tasks;
using System;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Server.Context;

namespace MultiFactor.Radius.Adapter.Core.Pipeline;

public class RadiusPipeline : IRadiusPipeline
{
    private readonly RadiusRequestDelegate _requestPipeline;
    private readonly ILogger<RadiusPipeline> _logger;

    public RadiusPipeline(RadiusRequestDelegate requestPipeline, ILogger<RadiusPipeline> logger)
    {
        _requestPipeline = requestPipeline ?? throw new ArgumentNullException(nameof(requestPipeline));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(RadiusContext context)
    {
        try
        {
            await _requestPipeline(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HandleRequest");
        }
    }
}
