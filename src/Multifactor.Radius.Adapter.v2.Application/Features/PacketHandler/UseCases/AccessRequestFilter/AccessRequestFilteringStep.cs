using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessRequestFilter;

internal sealed class AccessRequestFilteringStep : IRadiusPipelineStep
{
    private readonly ILogger<AccessRequestFilteringStep> _logger;
    private const string StepName = nameof(AccessRequestFilteringStep);
    
    public AccessRequestFilteringStep(ILogger<AccessRequestFilteringStep> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{StepName}' started", StepName);
        
        if (context.RequestPacket.Code == PacketCode.AccessRequest)
            return Task.CompletedTask;
        
        LogUnprocessablePacket(context);
        context.Terminate();
        context.SkipResponse();
        
        return Task.CompletedTask;
    }

    private void LogUnprocessablePacket(RadiusPipelineContext context)
    {
        var client = context.RequestPacket.ProxyEndpoint?.Address 
                     ?? context.RequestPacket.RemoteEndpoint?.Address;
        var clientInfo = client?.ToString() ?? "unknown";
        
        _logger.LogWarning(
            "Unprocessable packet type: {PacketCode}, from {Client}",
            context.RequestPacket.Code.ToString(),
            clientInfo);
    }
}