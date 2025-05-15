using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class StatusServerFilteringStep : IRadiusPipelineStep
{
    private readonly ApplicationVariables _applicationVariables;
    public StatusServerFilteringStep(ApplicationVariables applicationVariables)
    {
        _applicationVariables = applicationVariables;
    }

    public async Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
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