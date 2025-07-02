using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

namespace Multifactor.Radius.Adapter.v2.Core.AccessChallenge;

public class SecondFactorChallengeProcessor : IChallengeProcessor
{
    // TODO ConcurrentDictionary -> MemoryCache
    private readonly ConcurrentDictionary<ChallengeIdentifier, IRadiusPipelineExecutionContext> _challengeContexts = new();
    private readonly IMultifactorApiService _apiService;
    private readonly ILogger<SecondFactorChallengeProcessor> _logger;

    public ChallengeType ChallengeType => ChallengeType.SecondFactor;

    public SecondFactorChallengeProcessor(IMultifactorApiService apiAdapter, ILogger<SecondFactorChallengeProcessor> logger)
    {
        _apiService = apiAdapter;
        _logger = logger;
    }

    public ChallengeIdentifier AddChallengeContext(IRadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ResponseInformation.State);
        
        var id = new ChallengeIdentifier(context.ClientConfigurationName, context.ResponseInformation.State);
        if (_challengeContexts.TryAdd(id, context))
        {
            _logger.LogInformation("Challenge {State:l} was added for message id={id}", id.RequestId, context.RequestPacket.Identifier);
            return id;
        }

        _logger.LogError("Unable to cache request id={id} for the '{cfg:l}' configuration", context.RequestPacket.Identifier, context.ClientConfigurationName);
        return ChallengeIdentifier.Empty;
    }

    public bool HasChallengeContext(ChallengeIdentifier identifier) => _challengeContexts.ContainsKey(identifier);

    public async Task<ChallengeStatus> ProcessChallengeAsync(ChallengeIdentifier identifier, IRadiusPipelineExecutionContext context)
    {
        _logger.LogInformation("Processing challenge {State:l} for message id={id} from {host:l}:{port}",
            identifier.RequestId,
            context.RequestPacket.Identifier,
            context.RemoteEndpoint.Address,
            context.RemoteEndpoint.Port);

        var userName = context.RequestPacket.UserName;
        if (string.IsNullOrWhiteSpace(userName))
            return ProcessEmptyName(context, identifier.RequestId);

        var challengeStatus = ProcessAuthenticationType(context, context.Passphrase, identifier.RequestId, out var userAnswer);
        if (challengeStatus == ChallengeStatus.Reject)
            return challengeStatus;

        var challengeContext = GetChallengeContext(identifier) ?? throw new InvalidOperationException($"Challenge context with identifier '{identifier}' was not found");
        var response = await _apiService.SendChallengeAsync(new SendChallengeRequest(challengeContext, userAnswer!, identifier.RequestId));

        return ProcessResponse(context, challengeContext, response, identifier);
    }


    private IRadiusPipelineExecutionContext? GetChallengeContext(ChallengeIdentifier identifier)
    {
        if (_challengeContexts.TryGetValue(identifier, out IRadiusPipelineExecutionContext? request))
            return request;

        _logger.LogError("Unable to get cached request with state={identifier:l}", identifier);
        return null;
    }

    private void RemoveChallengeContext(ChallengeIdentifier identifier)
    {
        _challengeContexts.TryRemove(identifier, out _);
    }

    private ChallengeStatus ProcessAuthenticationType(IRadiusPipelineExecutionContext context, UserPassphrase passphrase, string requestId, out string? userAnswer)
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
                        context.RemoteEndpoint.Address,
                        context.RemoteEndpoint.Port);

                    context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
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
                        context.RemoteEndpoint.Address,
                        context.RemoteEndpoint.Port);

                    context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
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
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port,
                    context.RequestPacket.AuthenticationType);

                context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
                context.ResponseInformation.State = requestId;

                return ChallengeStatus.Reject;
        }
    }

    private ChallengeStatus ProcessResponse(IRadiusPipelineExecutionContext context, IRadiusPipelineExecutionContext challengeContext, MultifactorResponse response, ChallengeIdentifier identifier)
    {
        context.ResponseInformation.ReplyMessage = response.ReplyMessage;
        switch (response.Code)
        {
            case AuthenticationStatus.Accept:
                context.ResponsePacket = challengeContext.ResponsePacket;
                context.UserLdapProfile = challengeContext.UserLdapProfile;
                context.AuthenticationState.FirstFactorStatus = challengeContext.AuthenticationState.FirstFactorStatus;
                context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Accept;
                context.Passphrase = challengeContext.Passphrase;
                
                RemoveChallengeContext(identifier);
                
                _logger.LogDebug(
                    "Challenge {State:l} was processed for message id={id} from {host:l}:{port} with result '{Result}'",
                    identifier.RequestId,
                    context.RequestPacket.Identifier,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port,
                    response.Code);

                return ChallengeStatus.Accept;

            case AuthenticationStatus.Reject:
                RemoveChallengeContext(identifier);
                _logger.LogDebug(
                    "Challenge {State:l} was processed for message id={id} from {host:l}:{port} with result '{Result}'",
                    identifier.RequestId,
                    context.RequestPacket.Identifier,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port,
                    response.Code);

                context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
                context.ResponseInformation.State = identifier.RequestId;

                return ChallengeStatus.Reject;

            default:
                context.ResponseInformation.State = identifier.RequestId;

                return ChallengeStatus.InProcess;
        }
    }

    private ChallengeStatus ProcessEmptyName(IRadiusPipelineExecutionContext context, string requestId)
    {
        _logger.LogWarning(
            "Unable to process challenge {State:l} for message id={id} from {host:l}:{port}: Can't find User-Name",
            requestId,
            context.RequestPacket.Identifier,
            context.RemoteEndpoint.Address,
            context.RemoteEndpoint.Port);

        context.AuthenticationState.SecondFactorStatus = AuthenticationStatus.Reject;
        context.ResponseInformation.State = requestId;

        return ChallengeStatus.Reject;
    }
}