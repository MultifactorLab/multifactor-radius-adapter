using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor;

internal sealed class SecondFactorStep : IRadiusPipelineStep
{
    private readonly MultifactorApiService _multifactorApiService;//TODO переделать сервис
    private readonly IChallengeProcessorProvider _challengeProcessorProvider;
    private readonly ICheckMembership _checkMembership;
    private readonly ILogger<SecondFactorStep> _logger;
    public SecondFactorStep(MultifactorApiService multifactorApiService, 
        IChallengeProcessorProvider challengeProcessorProvider, 
        ICheckMembership checkMembership, 
        ILogger<SecondFactorStep> logger)
    {
        _multifactorApiService = multifactorApiService;
        _challengeProcessorProvider = challengeProcessorProvider;
        _logger = logger;
        _checkMembership = checkMembership;
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
                context.RequestPacket.RemoteEndpoint?.Address,
                context.RequestPacket.RemoteEndpoint?.Port);
            return false;
        }

        if (UnsupportedAccountType(context))
            return true;
        
        if (!ShouldBypassByGroups(context))
            return true;
        
        _logger.LogInformation("Second factor is bypassed for user {user:l} at '{domain:l}'", context.RequestPacket.UserName, context.LdapConfiguration?.ConnectionString);
        
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
            var userIdentity = new UserIdentity(context.RequestPacket.UserName);
            var domainInfo = context.ForestMetadata?.DetermineForestDomain(userIdentity);
            var request = MembershipDto.FromContext(context, serverConfig.SecondFaBypassGroups, domainInfo);
            bypassMember = context.LdapProfile!.MemberOf.Intersect(serverConfig.SecondFaBypassGroups).Any() || _checkMembership.Execute(request);
            if (bypassMember is true)
            {
                _logger.LogInformation("User '{user:l}' is a member of the 2FA bypass group in '{domain:l}'", context.RequestPacket.UserName, serverConfig.ConnectionString);
                return true;
            }
            _logger.LogInformation("User '{user:l}' is not a member of the 2FA bypass group in '{domain:l}'", context.RequestPacket.UserName, serverConfig.ConnectionString);
        }

        bool? secondFactorMember = null;
        if (serverConfig.SecondFaGroups.Any())
        {
            var userIdentity = new UserIdentity(context.RequestPacket.UserName);
            var domainInfo = context.ForestMetadata?.DetermineForestDomain(userIdentity);
            var request = MembershipDto.FromContext(context, serverConfig.SecondFaGroups, domainInfo);
            secondFactorMember = context.LdapProfile!.MemberOf.Intersect(serverConfig.SecondFaGroups).Any() || _checkMembership.Execute(request);
            if (secondFactorMember is true)
            {
                _logger.LogInformation("User '{user:l}' is a member of the 2FA group in '{domain:l}'", context.RequestPacket.UserName, serverConfig.ConnectionString);
            }
            else
            {
                _logger.LogInformation("User '{user:l}' is not a member of the 2FA group in '{domain:l}'", context.RequestPacket.UserName, serverConfig.ConnectionString);
            }
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
        
        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var domainInfo = context.ForestMetadata?.DetermineForestDomain(userIdentity);
        var request = MembershipDto.FromContext(context, context.LdapConfiguration.AuthenticationCacheGroups, domainInfo);
        var isMember = context.LdapProfile!.MemberOf.Intersect(context.LdapConfiguration.AuthenticationCacheGroups).Any() || _checkMembership.Execute(request);
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