using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class AccessRequestFilteringStep : IRadiusPipelineStep
{
    private readonly ILogger<AccessRequestFilteringStep> _logger;
    public AccessRequestFilteringStep(ILogger<AccessRequestFilteringStep> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        if (context.RequestPacket.Code == PacketCode.AccessRequest)
        {
            await Task.CompletedTask;
            return;
        }
        
        _logger.LogWarning("Unprocessable packet type: {code}", context.RequestPacket.Code);
        context.ExecutionState.Terminate();
        context.ExecutionState.SkipResponse();
    }
}