using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class SecondFactorStep : IRadiusPipelineStep
{
    private readonly IMultifactorApiService _multifactorApiService;
    private readonly IChallengeProcessorProvider _challengeProcessorProvider;
    private readonly ILdapGroupService _ldapGroupService;
    private readonly ILogger<SecondFactorStep> _logger;
    public SecondFactorStep(IMultifactorApiService multifactorApiService, IChallengeProcessorProvider challengeProcessorProvider, ILdapGroupService ldapGroupService, ILogger<SecondFactorStep> logger)
    {
        _multifactorApiService = multifactorApiService;
        _challengeProcessorProvider = challengeProcessorProvider;
        _ldapGroupService = ldapGroupService;
        _logger = logger;
    }

    public async Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(SecondFactorStep));
        ArgumentNullException.ThrowIfNull(context);
        
        if (!ShouldCallSecondFactor(context))
        {
            context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Bypass;
            await Task.CompletedTask;
            return;
        }

        var apiResponse = await _multifactorApiService.CreateSecondFactorRequestAsync(context);
        ProcessApiResponse(context, apiResponse);
    }

    private bool ShouldCallSecondFactor(IRadiusPipelineExecutionContext context)
    {
        if (context.AuthenticationState.SecondFactorStatus != AuthenticationStatus.Awaiting)
            return false;
        
        if (ShouldBypassByRequest(context))
        {
            _logger.LogInformation("Second factor is bypassed for user '{user:l}' from {host:l}:{port}",
                context.RequestPacket.UserName,
                context.RemoteEndpoint.Address,
                context.RemoteEndpoint.Port);
            return false;
        }

        if (ShouldBypassByGroups(context))
        {
            _logger.LogInformation("Second factor is bypassed for user {user:l} at '{domain:l}'", context.RequestPacket.UserName, context.Settings.LdapServerConfiguration.ConnectionString);
            return false;
        }
        
        return true;
    }

    private bool ShouldBypassByRequest(IRadiusPipelineExecutionContext context)
    {
        return context.RequestPacket.IsVendorAclRequest && context.Settings.FirstFactorAuthenticationSource == AuthenticationSource.Radius;
    }

    private bool ShouldBypassByGroups(IRadiusPipelineExecutionContext context)
    {
        var serverConfig = context.Settings.LdapServerConfiguration;
        bool? bypassMember = null;

        if (serverConfig.SecondFaBypassGroups.Any())
        {
            var request = GetMembershipRequest(context, serverConfig.SecondFaBypassGroups);
            bypassMember = _ldapGroupService.IsMemberOf(request);
        }

        if (bypassMember is true)
        {
            _logger.LogInformation("User '{user:l}' is a member of the 2FA bypass group in '{domain:l}'", context.RequestPacket.UserName, serverConfig.ConnectionString);
            return true;
        }

        bool? secondFactorMember = null;
        if (serverConfig.SecondFaGroups.Any())
        {
           var request = GetMembershipRequest(context, serverConfig.SecondFaGroups);
           secondFactorMember = _ldapGroupService.IsMemberOf(request);
           if (secondFactorMember is false)
               _logger.LogInformation("User '{user:l}' is not a member of the 2FA group at '{domain:l}'", context.RequestPacket.UserName, serverConfig.ConnectionString);
        }

        if (secondFactorMember.HasValue)
            return !secondFactorMember.Value;
        
        return false;
    }

    private void ProcessApiResponse(IRadiusPipelineExecutionContext context, MultifactorResponse apiResponse)
    {
        context.ResponseInformation.State = apiResponse.State;
        context.ResponseInformation.ReplyMessage = apiResponse.ReplyMessage;
        context.AuthenticationState.SecondFactorStatus = apiResponse.Code;

        if (apiResponse.Code != AuthenticationStatus.Awaiting) 
            return;
        
        var challengeProcessor = _challengeProcessorProvider.GetChallengeProcessorByType(ChallengeType.SecondFactor);
        if (challengeProcessor is null)
            throw new InvalidOperationException("Challenge processor could not be found");
        challengeProcessor.AddChallengeContext(context);
    }

    private MembershipRequest GetMembershipRequest(IRadiusPipelineExecutionContext context, IEnumerable<string> targetGroupsNames)
    {
        var groupDns = targetGroupsNames.Select(x => new DistinguishedName(x)).ToArray();
        
        return new MembershipRequest(
            context.UserLdapProfile.Dn,
            context.UserLdapProfile.MemberOf.ToArray(),
            context.Settings.LdapServerConfiguration.LoadNestedGroups,
            context.Settings.LdapServerConfiguration.NestedGroupsBaseDns.Select(x => new DistinguishedName(x)).ToArray(),
            context.Settings.LdapServerConfiguration.ConnectionString,
            context.Settings.LdapServerConfiguration.UserName,
            context.Settings.LdapServerConfiguration.Password,
            context.Settings.LdapServerConfiguration.BindTimeoutInSeconds,
            context.LdapSchema!,
            groupDns
        );
    }
}