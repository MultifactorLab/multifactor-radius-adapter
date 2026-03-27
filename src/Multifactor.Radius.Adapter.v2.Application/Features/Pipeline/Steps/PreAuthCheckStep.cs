using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class PreAuthCheckStep : IRadiusPipelineStep
{
    private readonly ILogger<PreAuthCheckStep> _logger;
    private readonly ICheckMembership _ldapAdapter;
    
    public PreAuthCheckStep(ILogger<PreAuthCheckStep> logger, ICheckMembership ldapAdapter)
    {
        _logger = logger;
        _ldapAdapter = ldapAdapter;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(PreAuthCheckStep));
        
        var isNeedOtp = SecondFaBypassGroupsDisableOrUserIsNotMemberOf(context);
        switch (context.ClientConfiguration.PreAuthenticationMethod)
        {
            case PreAuthMode.Otp when isNeedOtp && context.Passphrase?.Otp == null:
                context.SecondFactorStatus = AuthenticationStatus.Reject;
                _logger.LogError("Pre-auth second factor was rejected: otp code is empty. User '{user:l}' from {host:l}:{port}",
                    context.RequestPacket.UserName, 
                    context.RequestPacket.RemoteEndpoint.Address, 
                    context.RequestPacket.RemoteEndpoint.Port);
                context.Terminate();
                return Task.CompletedTask;
            
            case PreAuthMode.None:
            case PreAuthMode.Otp:
            case PreAuthMode.Any:
                _logger.LogDebug("Pre-auth check for '{user}' is completed.", context.RequestPacket.UserName);
                return Task.CompletedTask;
            
            default:
                throw new NotImplementedException($"Unknown pre-auth method: {context.ClientConfiguration.PreAuthenticationMethod}"); 
        }
    }
    
    private bool SecondFaBypassGroupsDisableOrUserIsNotMemberOf(RadiusPipelineContext context)
    {
        var serverConfig = context.LdapConfiguration;
        if (serverConfig is null)
            return true;
        
        if (!serverConfig.SecondFaBypassGroups.Any())
            return true;
        
        var request = MembershipDto.FromContext(context, serverConfig.SecondFaBypassGroups);
        var isMemberOfBypassGroups = _ldapAdapter.Execute(request);
        
        return !isMemberOfBypassGroups;
    }
}