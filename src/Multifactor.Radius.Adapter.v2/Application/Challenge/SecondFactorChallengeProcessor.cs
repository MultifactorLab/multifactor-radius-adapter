using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Challenge.Interface;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.Challenge;
using Multifactor.Radius.Adapter.v2.Domain.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Domain.MultifactorApi.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain.Radius;
using Multifactor.Radius.Adapter.v2.Infrastructure.Cache;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Dto;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Interface;
using Multifactor.Radius.Adapter.v2.Infrastructure.MultifactorApi.Dto;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Application.Challenge;

public class SecondFactorChallengeProcessor : IChallengeProcessor
{
    private readonly IMultifactorApiService _apiService;
    private readonly ILdapGroupService _ldapGroupService;
    private readonly ICacheService _cache;
    private readonly ILogger<SecondFactorChallengeProcessor> _logger;

    public ChallengeType ChallengeType => ChallengeType.SecondFactor;

    public SecondFactorChallengeProcessor(
        IMultifactorApiService apiService,
        ILdapGroupService groupService,
        ICacheService cache,
        ILogger<SecondFactorChallengeProcessor> logger)
    {
        _apiService = apiService;
        _ldapGroupService = groupService;
        _cache = cache;
        _logger = logger;
    }

    public ChallengeIdentifier AddChallengeContext(RadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        if (string.IsNullOrWhiteSpace(context.ResponseInformation.State))
            throw new ArgumentException("State is required", nameof(context));

        var identifier = new ChallengeIdentifier(context.ClientConfigurationName, context.ResponseInformation.State);
        
        _cache.Set(identifier.Value, context);
        _logger.LogInformation("Challenge {ChallengeId} added for message {MessageId}", 
            identifier.RequestId, context.RequestPacket.Identifier);

        return identifier;
    }

    public bool HasChallengeContext(ChallengeIdentifier identifier) => 
        _cache.TryGetValue<RadiusPipelineExecutionContext>(identifier.Value, out _);

    public async Task<ChallengeStatus> ProcessChallengeAsync(
        ChallengeIdentifier identifier, 
        RadiusPipelineExecutionContext context)
    {
        _logger.LogInformation("Processing challenge {ChallengeId} for message {MessageId}", 
            identifier.RequestId, context.RequestPacket.Identifier);

        var userName = context.RequestPacket.UserName;
        if (string.IsNullOrWhiteSpace(userName))
            return ProcessEmptyUserName(context, identifier.RequestId);

        var userAnswer = GetUserAnswer(context);
        if (userAnswer == null)
            return ProcessInvalidAuthData(context, identifier.RequestId);

        var cachedContext = GetCachedContext(identifier);
        if (cachedContext == null)
            return ChallengeStatus.Reject;

        var shouldCacheResponse = CheckCacheEligibility(context);
        var request = new SendChallengeRequest(cachedContext, userAnswer, identifier.RequestId, shouldCacheResponse);
        
        var response = await _apiService.SendChallengeAsync(request);
        
        return HandleApiResponse(context, cachedContext, response, identifier);
    }

    private static string? GetUserAnswer(RadiusPipelineExecutionContext context)
    {
        switch (context.RequestPacket.AuthenticationType)
        {
            case AuthenticationType.PAP:
                return context.Passphrase.Raw;

            case AuthenticationType.MSCHAP2:
                var msChapResponse = context.RequestPacket.GetAttribute<byte[]>("MS-CHAP2-Response");
                if (msChapResponse == null || msChapResponse.Length < 8)
                    return null;

                var otpData = msChapResponse.Skip(2).Take(6).ToArray();
                return Encoding.ASCII.GetString(otpData);

            default:
                return null;
        }
    }

    private RadiusPipelineExecutionContext? GetCachedContext(ChallengeIdentifier identifier)
    {
        if (_cache.TryGetValue<RadiusPipelineExecutionContext>(identifier.Value, out var context))
            return context;

        _logger.LogError("Cached context not found for challenge {ChallengeId}", identifier);
        return null;
    }

    private ChallengeStatus ProcessEmptyUserName(RadiusPipelineExecutionContext context, string state)
    {
        _logger.LogWarning("User-Name not found for challenge {State}", state);
        
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
        context.ResponseInformation.State = state;

        return ChallengeStatus.Reject;
    }

    private ChallengeStatus ProcessInvalidAuthData(RadiusPipelineExecutionContext context, string state)
    {
        _logger.LogWarning("Invalid authentication data for challenge {State}", state);
        
        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
        context.ResponseInformation.State = state;

        return ChallengeStatus.Reject;
    }

    private bool CheckCacheEligibility(RadiusPipelineExecutionContext context)
    {
        if (context.LdapServerConfiguration?.AuthenticationCacheGroups == null || 
            context.LdapServerConfiguration.AuthenticationCacheGroups.Count == 0)
            return true;

        var request = new MembershipRequest(context, context.LdapServerConfiguration.AuthenticationCacheGroups);
        var isMember = _ldapGroupService.IsMemberOf(request);
        
        _logger.LogDebug("User {UserName} cache eligibility: {IsMember}", 
            context.RequestPacket.UserName, isMember);
        
        return isMember;
    }

    private ChallengeStatus HandleApiResponse(
        RadiusPipelineExecutionContext currentContext,
        RadiusPipelineExecutionContext cachedContext,
        MultifactorResponse response,
        ChallengeIdentifier identifier)
    {
        currentContext.ResponseInformation.ReplyMessage = response.ReplyMessage;
        
        switch (response.Code)
        {
            case AuthenticationStatus.Accept:
                ApplyCachedData(currentContext, cachedContext);
                _cache.Remove(identifier.Value);
                
                _logger.LogInformation("Challenge {ChallengeId} accepted", identifier.RequestId);
                return ChallengeStatus.Accept;

            case AuthenticationStatus.Reject:
                _cache.Remove(identifier.Value);
                
                _logger.LogInformation("Challenge {ChallengeId} rejected", identifier.RequestId);
                return ChallengeStatus.Reject;

            default:
                currentContext.ResponseInformation.State = identifier.RequestId;
                return ChallengeStatus.InProcess;
        }
    }

    private static void ApplyCachedData(RadiusPipelineExecutionContext target, RadiusPipelineExecutionContext source)
    {
        target.ResponsePacket = source.ResponsePacket;
        target.UserLdapProfile = source.UserLdapProfile;
        target.AuthenticationState.FirstFactorStatus = source.AuthenticationState.FirstFactorStatus;
        target.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Accept;
        target.Passphrase = source.Passphrase;
    }
}