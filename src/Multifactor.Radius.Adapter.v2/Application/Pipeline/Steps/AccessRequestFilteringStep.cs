using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps;

public class AccessRequestFilteringStep : IRadiusPipelineStep
{
    private readonly ILogger<AccessRequestFilteringStep> _logger;
    public AccessRequestFilteringStep(ILogger<AccessRequestFilteringStep> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(RadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(AccessRequestFilteringStep));
        if (context.RequestPacket.Code == PacketCode.AccessRequest)
        {
            await Task.CompletedTask;
            return;
        }

        var client = context.ProxyEndpoint?.Address ?? context.RemoteEndpoint.Address;
        _logger.LogWarning("Unprocessable packet type: {code:l}, from {client:l}", context.RequestPacket.Code.ToString(), client.ToString());
        context.ExecutionState.Terminate();
        context.ExecutionState.SkipResponse();
    }
}