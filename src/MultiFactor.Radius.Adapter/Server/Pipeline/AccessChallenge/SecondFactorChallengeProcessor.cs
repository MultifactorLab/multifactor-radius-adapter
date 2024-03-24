//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge
{
    public class SecondFactorChallengeProcessor : ISecondFactorChallengeProcessor
    {
        private readonly ConcurrentDictionary<ChallengeRequestIdentifier, RadiusContext> _stateChallengePendingRequests = new();
        private readonly IMultiFactorApiClient _multiFactorApiClient;
        private readonly ILogger<SecondFactorChallengeProcessor> _logger;

        public SecondFactorChallengeProcessor(IMultiFactorApiClient multiFactorApiClient, ILogger<SecondFactorChallengeProcessor> logger)
        {
            _multiFactorApiClient = multiFactorApiClient ?? throw new ArgumentNullException(nameof(multiFactorApiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verify one time password from user input
        /// </summary>
        public async Task<ChallengeCode> ProcessChallengeAsync(ChallengeRequestIdentifier identifier, RadiusContext context)
        {
            _logger.LogInformation("Processing challenge {State:l} for message id={id} from {host:l}:{port}", 
                identifier.RequestId,
                context.Header.Identifier,
                context.RemoteEndpoint.Address,
                context.RemoteEndpoint.Port);

            if (string.IsNullOrEmpty(context.UserName))
            {
                _logger.LogWarning("Unable to process challenge {State:l} for message id={id} from {host:l}:{port}: Can't find User-Name",
                    identifier.RequestId, 
                    context.Header.Identifier,
                    context.RemoteEndpoint.Address,
                    context.RemoteEndpoint.Port);
                return ChallengeCode.Reject;
            }

            string userAnswer;
            switch (context.RequestPacket.AuthenticationType)
            {
                case AuthenticationType.PAP:
                    //user-password attribute holds second request challenge from user
                    userAnswer = context.Passphrase.Raw;

                    if (string.IsNullOrEmpty(userAnswer))
                    {
                        _logger.LogWarning("Can't find User-Password with user response in message id={id} from {host:l}:{port}", 
                            context.Header.Identifier, 
                            context.RemoteEndpoint.Address, 
                            context.RemoteEndpoint.Port);
                        return ChallengeCode.Reject;
                    }

                    break;
                case AuthenticationType.MSCHAP2:
                    var msChapResponse = context.RequestPacket.GetAttribute<byte[]>("MS-CHAP2-Response");

                    if (msChapResponse == null)
                    {
                        _logger.LogWarning("Unable to process challenge {State:l} for message id={id} from {host:l}:{port}: Can't find MS-CHAP2-Response",
                            identifier.RequestId, 
                            context.Header.Identifier, 
                            context.RemoteEndpoint.Address,
                            context.RemoteEndpoint.Port);
                        return ChallengeCode.Reject;
                    }

                    //forti behaviour
                    var otpData = msChapResponse.Skip(2).Take(6).ToArray();
                    userAnswer = Encoding.ASCII.GetString(otpData);

                    break;
                default:
                    _logger.LogWarning("Unable to process challenge {State:l} for message id={id} from {host:l}:{port}: Unsupported authentication type '{Auth}'",
                        identifier.RequestId,
                        context.Header.Identifier,
                        context.RemoteEndpoint.Address,
                        context.RemoteEndpoint.Port,
                        context.RequestPacket.AuthenticationType);
                    return ChallengeCode.Reject;
            }

            // copy initial request profile to challenge request context
            var stateChallengePendingRequest = GetStateChallengeRequest(identifier);
            stateChallengePendingRequest?.CopyProfileToContext(context);

            var response = await _multiFactorApiClient.Challenge(context, userAnswer, identifier);
            context.ReplyMessage = response.ReplyMessage;
            switch (response.Code)
            {
                case PacketCode.AccessAccept:
                    if (stateChallengePendingRequest != null)
                    {
                        context.UserGroups = stateChallengePendingRequest.UserGroups;
                        context.ResponsePacket = stateChallengePendingRequest.ResponsePacket;
                        context.LdapAttrs = stateChallengePendingRequest.LdapAttrs;
                    }

                    RemoveStateChallengeRequest(identifier);
                    _logger.LogDebug("Challenge {State:l} was processed for message id={id} from {host:l}:{port} with result '{Result}'",
                        identifier.RequestId,
                        context.Header.Identifier,
                        context.RemoteEndpoint.Address,
                        context.RemoteEndpoint.Port, 
                        response.Code);
                    return ChallengeCode.Accept;

                case PacketCode.AccessReject:
                    RemoveStateChallengeRequest(identifier);
                    _logger.LogDebug("Challenge {State:l} was processed for message id={id} from {host:l}:{port} with result '{Result}'",
                        identifier.RequestId,
                        context.Header.Identifier,
                        context.RemoteEndpoint.Address,
                        context.RemoteEndpoint.Port, 
                        response.Code);
                    return ChallengeCode.Reject;
            }

            return ChallengeCode.InProcess;
        }

        public bool HasState(ChallengeRequestIdentifier identifier)
        {
            return _stateChallengePendingRequests.ContainsKey(identifier);
        }

        /// <summary>
        /// Add authenticated request to local cache for otp/challenge
        /// </summary>
        public void AddState(ChallengeRequestIdentifier identifier, RadiusContext context)
        {
            if (!_stateChallengePendingRequests.TryAdd(identifier, context))
            {
                _logger.LogError("Unable to cache request id={id} for the '{cfg:l}' configuration",
                    context.RequestPacket.Header.Identifier, context.Configuration.Name);
            }
        }

        /// <summary>
        /// Get authenticated request from local cache for otp/challenge
        /// </summary>
        private RadiusContext GetStateChallengeRequest(ChallengeRequestIdentifier identifier)
        {
            if (_stateChallengePendingRequests.TryGetValue(identifier, out RadiusContext request))
            {
                return request;
            }

            _logger.LogError($"Unable to get cached request with state={identifier}");
            return null;
        }

        /// <summary>
        /// Remove authenticated request from local cache
        /// </summary>
        /// <param name="state"></param>
        private void RemoveStateChallengeRequest(ChallengeRequestIdentifier identifier)
        {
            _stateChallengePendingRequests.TryRemove(identifier, out RadiusContext _);
        }
    }
}