using MultiFactor.Radius.Adapter.Server;
using System.Threading.Tasks;
using System;
using Serilog;
using MultiFactor.Radius.Adapter.Core.Exceptions;

namespace MultiFactor.Radius.Adapter.Core.Pipeline;

public class RadiusPipeline : IRadiusPipeline
{
    private readonly RadiusRequestDelegate _requestPipeline;
    private readonly ILogger _logger;

    public RadiusPipeline(RadiusRequestDelegate requestPipeline, ILogger logger)
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
            _logger.Error(ex, "HandleRequest");
        }
    }
}
