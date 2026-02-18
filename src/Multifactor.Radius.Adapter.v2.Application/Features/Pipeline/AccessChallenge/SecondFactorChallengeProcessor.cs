using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge;

public class SecondFactorChallengeProcessor : IChallengeProcessor
{
    private readonly IMemoryCache _memoryCache;
    private readonly MultifactorApiService _apiService;
    private readonly ILdapAdapter _ldapAdapter;
    private readonly ILogger<SecondFactorChallengeProcessor> _logger;
    private readonly TimeSpan _defaultCacheDuration = TimeSpan.FromMinutes(10);
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public ChallengeType ChallengeType => ChallengeType.SecondFactor;

    public SecondFactorChallengeProcessor(
        MultifactorApiService apiAdapter, 
        ILdapAdapter ldapAdapter, 
        ILogger<SecondFactorChallengeProcessor> logger,
        IMemoryCache memoryCache)
    {
        _apiService = apiAdapter;
        _ldapAdapter = ldapAdapter;
        _logger = logger;
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        
        var cacheDuration = _defaultCacheDuration;
        _cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheDuration,
            SlidingExpiration = null, //security sensitive
            Priority = CacheItemPriority.Normal,
            Size = 1,
            PostEvictionCallbacks =
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (key, value, reason, state) =>
                    {
                        if (value is RadiusPipelineContext context)
                        {
                            logger.LogDebug("Challenge context evicted: {Key}, reason: {Reason}, message id={Id}",
                                key, reason, context.RequestPacket.Identifier);
                        }
                    }
                }
            }
        };
    }

    public ChallengeIdentifier AddChallengeContext(RadiusPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ResponseInformation.State);
        
        var id = new ChallengeIdentifier(context.ClientConfiguration.Name, context.ResponseInformation.State);
        var key = CreateCacheKey(id);
        
        try
        {
            _memoryCache.Set(key, context, _cacheOptions);
            _logger.LogInformation("Challenge {State:l} was added for message id={id} (cached until {expiration})", 
                id.RequestId, 
                context.RequestPacket.Identifier,
                DateTime.UtcNow.Add(_cacheOptions.AbsoluteExpirationRelativeToNow!.Value));
            
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to cache request id={id} for the '{cfg:l}' configuration", 
                context.RequestPacket.Identifier, 
                context.ClientConfiguration.Name);
            return ChallengeIdentifier.Empty;
        }
    }

    public bool HasChallengeContext(ChallengeIdentifier identifier)
    {
        var key = CreateCacheKey(identifier);
        return _memoryCache.TryGetValue(key, out _);
    }

    public async Task<ChallengeStatus> ProcessChallengeAsync(ChallengeIdentifier identifier, RadiusPipelineContext context)
    {
        _logger.LogInformation("Processing challenge {State:l} for message id={id} from {host:l}:{port}",
            identifier.RequestId,
            context.RequestPacket.Identifier,
            context.RequestPacket.RemoteEndpoint.Address,
            context.RequestPacket.RemoteEndpoint.Port);

        var userName = context.RequestPacket.UserName;
        if (string.IsNullOrWhiteSpace(userName))
            return ProcessEmptyName(context, identifier.RequestId);

        var challengeStatus = ProcessAuthenticationType(context, context.Passphrase, identifier.RequestId, out var userAnswer);
        if (challengeStatus == ChallengeStatus.Reject)
            return challengeStatus;

        var challengeContext = GetChallengeContext(identifier) ?? throw new InvalidOperationException($"Challenge context with identifier '{identifier}' was not found");
        var shouldCacheResponse = ShouldCacheResponse(context);
        var response = await _apiService.SendChallengeAsync(challengeContext, shouldCacheResponse, identifier.RequestId, userAnswer!);

        return ProcessResponse(context, challengeContext, response, identifier);
    }

    private RadiusPipelineContext? GetChallengeContext(ChallengeIdentifier identifier)
    {
        var key = CreateCacheKey(identifier);
        
        if (_memoryCache.TryGetValue(key, out RadiusPipelineContext? context))
        {
            _logger.LogDebug("Retrieved challenge context for {State:l}", identifier.RequestId);
            return context;
        }

        _logger.LogError("Unable to get cached request with state={identifier:l}", identifier);
        return null;
    }

    private void RemoveChallengeContext(ChallengeIdentifier identifier)
    {
        var key = CreateCacheKey(identifier);
        _memoryCache.Remove(key);
        _logger.LogDebug("Removed challenge context for {State:l}", identifier.RequestId);
    }

    private static string CreateCacheKey(ChallengeIdentifier identifier)
    {
        return $"Challenge:{identifier.ToString()}";
    }
    
    private ChallengeStatus ProcessAuthenticationType(RadiusPipelineContext context, UserPassphrase passphrase, string requestId, out string? userAnswer)
    {
        userAnswer = string.Empty;
        switch (context.RequestPacket.AuthenticationType)
        {
            case AuthenticationType.PAP:
                //user-password attribute holds second request challenge from user
                userAnswer = passphrase.Raw;

                if (string.IsNullOrWhiteSpace(userAnswer))
                {
                    _logger.LogWarning(
                        "Can't find User-Password with user response in message id={id} from {host:l}:{port}",
                        context.RequestPacket.Identifier,
                        context.RequestPacket.RemoteEndpoint.Address,
                        context.RequestPacket.RemoteEndpoint.Port);

                    context.SecondFactorStatus = AuthenticationStatus.Reject;
                    context.ResponseInformation.State = requestId;

                    return ChallengeStatus.Reject;
                }

                return ChallengeStatus.InProcess;
            case AuthenticationType.MSCHAP2:
                var msChapResponse = context.RequestPacket.GetAttribute<byte[]?>("MS-CHAP2-Response");

                if (msChapResponse == null)
                {
                    _logger.LogWarning(
                        "Unable to process challenge {State:l} for message id={id} from {host:l}:{port}: Can't find MS-CHAP2-Response",
                        requestId,
                        context.RequestPacket.Identifier,
                        context.RequestPacket.RemoteEndpoint.Address,
                        context.RequestPacket.RemoteEndpoint.Port);

                    context.SecondFactorStatus = AuthenticationStatus.Reject;
                    context.ResponseInformation.State = requestId;

                    return ChallengeStatus.Reject;
                }

                //forti behaviour
                var otpData = msChapResponse.Skip(2).Take(6).ToArray();
                userAnswer = Encoding.ASCII.GetString(otpData);
                return ChallengeStatus.InProcess;
            default:
                _logger.LogWarning(
                    "Unable to process challenge {State:l} for message id={id} from {host:l}:{port}: Unsupported authentication type '{Auth}'",
                    requestId,
                    context.RequestPacket.Identifier,
                    context.RequestPacket.RemoteEndpoint.Address,
                    context.RequestPacket.RemoteEndpoint.Port,
                    context.RequestPacket.AuthenticationType);

                context.SecondFactorStatus = AuthenticationStatus.Reject;
                context.ResponseInformation.State = requestId;

                return ChallengeStatus.Reject;
        }
    }

    private ChallengeStatus ProcessResponse(RadiusPipelineContext context, RadiusPipelineContext challengeContext, SecondFactorResponse response, ChallengeIdentifier identifier)
    {
        context.ResponseInformation.ReplyMessage = response.ReplyMessage;
        switch (response.Code)
        {
            case AuthenticationStatus.Accept:
                context.ResponsePacket = challengeContext.ResponsePacket;
                context.LdapProfile = challengeContext.LdapProfile;
                context.FirstFactorStatus = challengeContext.FirstFactorStatus;
                context.SecondFactorStatus = AuthenticationStatus.Accept;
                context.Passphrase = challengeContext.Passphrase;

                RemoveChallengeContext(identifier);
                
                _logger.LogDebug(
                    "Challenge {State:l} was processed for message id={id} from {host:l}:{port} with result '{Result}'",
                    identifier.RequestId,
                    context.RequestPacket.Identifier,
                    context.RequestPacket.RemoteEndpoint.Address,
                    context.RequestPacket.RemoteEndpoint.Port,
                    response.Code);

                return ChallengeStatus.Accept;

            case AuthenticationStatus.Reject:
                RemoveChallengeContext(identifier);
                _logger.LogDebug(
                    "Challenge {State:l} was processed for message id={id} from {host:l}:{port} with result '{Result}'",
                    identifier.RequestId,
                    context.RequestPacket.Identifier,
                    context.RequestPacket.RemoteEndpoint.Address,
                    context.RequestPacket.RemoteEndpoint.Port,
                    response.Code);

                context.SecondFactorStatus = AuthenticationStatus.Reject;
                context.ResponseInformation.State = identifier.RequestId;

                return ChallengeStatus.Reject;

            default:
                context.ResponseInformation.State = identifier.RequestId;

                return ChallengeStatus.InProcess;
        }
    }

    private ChallengeStatus ProcessEmptyName(RadiusPipelineContext context, string requestId)
    {
        _logger.LogWarning(
            "Unable to process challenge {State:l} for message id={id} from {host:l}:{port}: Can't find User-Name",
            requestId,
            context.RequestPacket.Identifier,
            context.RequestPacket.RemoteEndpoint.Address,
            context.RequestPacket.RemoteEndpoint.Port);

        context.SecondFactorStatus = AuthenticationStatus.Reject;
        context.ResponseInformation.State = requestId;

        return ChallengeStatus.Reject;
    }
    
    private bool ShouldCacheResponse(RadiusPipelineContext context)
    {
        if (context.LdapConfiguration is null || context.LdapConfiguration.AuthenticationCacheGroups.Count == 0)
            return true;
        
        var cacheGroups = context.LdapConfiguration.AuthenticationCacheGroups;
        if (context.LdapProfile.MemberOf.Intersect(cacheGroups).Any()) return true;
        var isMember = _ldapAdapter.IsMemberOf(MembershipRequest.FromContext(context, cacheGroups));
        var groupsStr = string.Join(',', cacheGroups);
        var username = context.RequestPacket.UserName;
        _logger.LogDebug(
            !isMember
                ? "User '{userName}' is not a member of any authentication cache groups: ({groups})"
                : "User '{userName}' is a member of authentication cache groups: ({groups})", username, groupsStr);

        return isMember;
    }
}