using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class SecondFactorStep : IRadiusPipelineStep
{
    private readonly MultifactorApiService _multifactorApiService;
    private readonly IChallengeProcessorProvider _challengeProcessorProvider;
    private readonly ILdapAdapter _ldapAdapter;
    private readonly ILogger<SecondFactorStep> _logger;
    public SecondFactorStep(MultifactorApiService multifactorApiService, IChallengeProcessorProvider challengeProcessorProvider, ILdapAdapter ldapAdapter, ILogger<SecondFactorStep> logger)
    {
        _multifactorApiService = multifactorApiService;
        _challengeProcessorProvider = challengeProcessorProvider;
        _ldapAdapter = ldapAdapter;
        _logger = logger;
    }

    public async Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(SecondFactorStep));
        ArgumentNullException.ThrowIfNull(context);
        
        if (!ShouldCallSecondFactor(context))
        {
            context.SecondFactorStatus = AuthenticationStatus.Bypass;
            await Task.CompletedTask;
            return;
        }
        var shouldCacheApiResponse = ShouldCacheResponse(context);
        var apiResponse = await _multifactorApiService.CreateSecondFactorRequestAsync(context, shouldCacheApiResponse);
        ProcessApiResponse(context, apiResponse);
    }

    private bool ShouldCallSecondFactor(RadiusPipelineContext context)
    {
        if (context.SecondFactorStatus != AuthenticationStatus.Awaiting)
            return false;
        
        if (ShouldBypassByRequest(context))
        {
            _logger.LogInformation("Second factor is bypassed for user '{user:l}' from {host:l}:{port}",
                context.RequestPacket.UserName,
                context.RequestPacket.RemoteEndpoint.Address,
                context.RequestPacket.RemoteEndpoint.Port);
            return false;
        }

        if (UnsupportedAccountType(context))
            return true;
        
        if (!ShouldBypassByGroups(context))
            return true;
        
        _logger.LogInformation("Second factor is bypassed for user {user:l} at '{domain:l}'", context.RequestPacket.UserName, context.LdapConfiguration.ConnectionString);
        
        return false;
    }

    private static bool ShouldBypassByRequest(RadiusPipelineContext context)
    {
        return context.RequestPacket.IsVendorAclRequest && context.ClientConfiguration.FirstFactorAuthenticationSource == AuthenticationSource.Radius;
    }

    private bool ShouldBypassByGroups(RadiusPipelineContext context)
    {
        var serverConfig = context.LdapConfiguration;
        
        if (serverConfig is null)
            return false;
        
        bool? bypassMember = null;

        if (serverConfig.SecondFaBypassGroups.Any())
        {
            var request = MembershipRequest.FromContext(context, serverConfig.SecondFaBypassGroups);
            bypassMember = context.LdapProfile.MemberOf.Intersect(serverConfig.SecondFaBypassGroups).Any() || _ldapAdapter.IsMemberOf(request);
        }

        if (bypassMember is true)
        {
            _logger.LogInformation("User '{user:l}' is a member of the 2FA bypass group in '{domain:l}'", context.RequestPacket.UserName, serverConfig.ConnectionString);
            return true;
        }

        bool? secondFactorMember = null;
        if (serverConfig.SecondFaGroups.Any())
        {
            var request = MembershipRequest.FromContext(context, serverConfig.SecondFaGroups);
            secondFactorMember = context.LdapProfile.MemberOf.Intersect(serverConfig.SecondFaGroups).Any() || _ldapAdapter.IsMemberOf(request);
            if (secondFactorMember is false)
                _logger.LogInformation("User '{user:l}' is not a member of the 2FA group at '{domain:l}'", context.RequestPacket.UserName, serverConfig.ConnectionString);
        }

        if (secondFactorMember.HasValue)
            return !secondFactorMember.Value;
        
        return false;
    }

    private bool ShouldCacheResponse(RadiusPipelineContext context)
    {
        if (context.LdapConfiguration is null || context.LdapConfiguration.AuthenticationCacheGroups.Count == 0)
            return true;
        
        if (!context.IsDomainAccount)
        {
            _logger.LogInformation(
                "User '{user}' used '{accountType}' account to log in. Authentication cache groups check is skipped.",
                context.RequestPacket.UserName,
                context.RequestPacket.AccountType);
            return false;
        }
        
        var request = MembershipRequest.FromContext(context, context.LdapConfiguration.AuthenticationCacheGroups);
        var isMember = context.LdapProfile.MemberOf.Intersect(context.LdapConfiguration.AuthenticationCacheGroups).Any() || _ldapAdapter.IsMemberOf(request);
        var groupsStr = string.Join(',', context.LdapConfiguration.AuthenticationCacheGroups);
        var username = context.RequestPacket.UserName;
        if (!isMember)
            _logger.LogDebug("User '{userName}' is not a member of any authentication cache groups: ({groups})", username, groupsStr);
        else
            _logger.LogDebug("User '{userName}' is a member of authentication cache groups: ({groups})", username, groupsStr);

        return isMember;
    }

    private void ProcessApiResponse(RadiusPipelineContext context, SecondFactorResponse apiResponse)
    {
        context.SecondFactorStatus = apiResponse.Code;
        context.ResponseInformation.State = apiResponse.State;
        context.ResponseInformation.ReplyMessage = apiResponse.ReplyMessage;

        if (apiResponse.Code != AuthenticationStatus.Awaiting)
            return;

        var challengeProcessor = _challengeProcessorProvider.GetChallengeProcessorByType(ChallengeType.SecondFactor);
        if (challengeProcessor is null)
            throw new InvalidOperationException("Challenge processor could not be found");
        challengeProcessor.AddChallengeContext(context);
    }

    private bool UnsupportedAccountType(RadiusPipelineContext context)
    {
        if (context.IsDomainAccount)
            return false;
        
        _logger.LogInformation(
            "User '{user}' used '{accountType}' account to log in. Second factor groups check is skipped.",
            context.RequestPacket.UserName,
            context.RequestPacket.AccountType);

        return true;
    }
}