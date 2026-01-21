using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class StatusServerFilteringStep : IRadiusPipelineStep
{
    private readonly ApplicationVariables _applicationVariables;
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
        context.ResponseInformation.ReplyMessage = $"Server up {uptime.Days} days {uptime:hh\\:mm\\:ss}, ver.: {_applicationVariables.AppVersion}";
        context.FirstFactorStatus = AuthenticationStatus.Accept;
        context.SecondFactorStatus = AuthenticationStatus.Accept;
        context.Terminate();
    }
}