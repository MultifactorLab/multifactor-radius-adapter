using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.StatusServerFilter;

internal sealed class StatusServerFilteringStep : IRadiusPipelineStep
{
    private readonly ApplicationVariables _applicationVariables; //TODO
    private readonly ILogger<StatusServerFilteringStep> _logger;
    public StatusServerFilteringStep(ApplicationVariables applicationVariables, ILogger<StatusServerFilteringStep> logger)
    {
        _applicationVariables = applicationVariables;
        _logger = logger;
    }

    public async Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(StatusServerFilteringStep));
        var packet = context.RequestPacket;
        if (packet.Code != PacketCode.StatusServer)
        {
             await Task.CompletedTask;
             return;
        }
        
        var uptime = _applicationVariables.UpTime;
        context.ResponseInformation.ReplyMessage = $@"Server up {uptime.Days} days {uptime:hh\:mm\:ss}, ver.: {_applicationVariables.AppVersion}";
        context.FirstFactorStatus = AuthenticationStatus.Accept;
        context.SecondFactorStatus = AuthenticationStatus.Accept;
        context.Terminate();
    }
}