using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.PreAuthCheck;

internal sealed class PreAuthCheckStep : IRadiusPipelineStep
{
    private readonly ICheckMembership _checkMembership;
    private readonly ILogger<PreAuthCheckStep> _logger;
    
    public PreAuthCheckStep(ICheckMembership checkMembership, ILogger<PreAuthCheckStep> logger)
    {
        _checkMembership = checkMembership;
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(PreAuthCheckStep));

        if (context.ClientConfiguration.PreAuthenticationMethod is PreAuthMode.Otp)
        {
            var isNeedOtp = SecondFaBypassGroupsDisableOrUserIsNotMemberOf(context);
            if (isNeedOtp && context.Passphrase?.Otp is null)
            {
                _logger.LogError("Pre-auth second factor was rejected: otp code is empty. User '{user:l}' from {host:l}:{port}",
                    context.RequestPacket.UserName, 
                    context.RequestPacket.RemoteEndpoint?.Address, 
                    context.RequestPacket.RemoteEndpoint?.Port);
                context.Terminate();
                return Task.CompletedTask;
            }
        }
        _logger.LogDebug("Pre-auth check for '{user}' is completed.", context.RequestPacket.UserName);
        return Task.CompletedTask;
    }
    
    private bool SecondFaBypassGroupsDisableOrUserIsNotMemberOf(RadiusPipelineContext context)
    {
        var serverConfig = context.LdapConfiguration;
        if (serverConfig is null || !serverConfig.SecondFaBypassGroups.Any())
            return true;
        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var domainInfo = context.ForestMetadata?.DetermineForestDomain(userIdentity);

        var request = MembershipDto.FromContext(context, serverConfig.SecondFaBypassGroups, domainInfo);
        var isMemberOfBypassGroups = _checkMembership.Execute(request);
        
        return !isMemberOfBypassGroups;
    }
}