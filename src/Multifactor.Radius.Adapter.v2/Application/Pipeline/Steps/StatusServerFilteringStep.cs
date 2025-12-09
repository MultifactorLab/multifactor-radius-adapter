using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps;

public class StatusServerFilteringStep : IRadiusPipelineStep
{
    private readonly ApplicationVariables _applicationVariables;
    private readonly ILogger<StatusServerFilteringStep> _logger;
    public StatusServerFilteringStep(ApplicationVariables applicationVariables, ILogger<StatusServerFilteringStep> logger)
    {
        _applicationVariables = applicationVariables;
        _logger = logger;
    }

    public async Task ExecuteAsync(RadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(StatusServerFilteringStep));
        var packet = context.RequestPacket;
        if (packet.Code != PacketCode.StatusServer)
        {
             await Task.CompletedTask;
             return;
        }
        
        var uptime = _applicationVariables.UpTime;
        context.ResponseInformation.ReplyMessage = $"Server up {uptime.Days} days {uptime:hh\\:mm\\:ss}, ver.: {_applicationVariables.AppVersion}";
        context.AuthenticationState.FirstFactorStatus = AuthenticationStatus.Accept;
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Accept;
        context.ExecutionState.Terminate();
    }
}