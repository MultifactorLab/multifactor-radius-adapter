//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server
{
    public class ChallengeProcessor : IChallengeProcessor
    {
        private readonly ConcurrentDictionary<ChallengeRequestIdentifier, RadiusContext> _stateChallengePendingRequests = new();
        private readonly IMultiFactorApiClient _multiFactorApiClient;
        private readonly ILogger<ChallengeProcessor> _logger;

        public ChallengeProcessor(IMultiFactorApiClient multiFactorApiClient, ILogger<ChallengeProcessor> logger)
        {
            _multiFactorApiClient = multiFactorApiClient ?? throw new ArgumentNullException(nameof(multiFactorApiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verify one time password from user input
        /// </summary>
        public async Task<PacketCode> ProcessChallengeAsync(ChallengeRequestIdentifier identifier, RadiusContext context)
        {
            var userName = context.UserName;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}", context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            string userAnswer;

            switch (context.RequestPacket.AuthenticationType)
            {
                case AuthenticationType.PAP:
                    //user-password attribute holds second request challenge from user
                    userAnswer = context.RequestPacket.GetString("User-Password");

                    if (string.IsNullOrEmpty(userAnswer))
                    {
                        _logger.LogWarning("Can't find User-Password with user response in message id={id} from {host:l}:{port}", context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                        return PacketCode.AccessReject;
                    }

                    break;
                case AuthenticationType.MSCHAP2:
                    var msChapResponse = context.RequestPacket.GetAttribute<byte[]>("MS-CHAP2-Response");

                    if (msChapResponse == null)
                    {
                        _logger.LogWarning("Can't find MS-CHAP2-Response in message id={id} from {host:l}:{port}", context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                        return PacketCode.AccessReject;
                    }

                    //forti behaviour
                    var otpData = msChapResponse.Skip(2).Take(6).ToArray();
                    userAnswer = Encoding.ASCII.GetString(otpData);

                    break;
                default:
                    _logger.LogWarning("Unable to process {auth} challange in message id={id} from {host:l}:{port}", context.RequestPacket.AuthenticationType, context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                    return PacketCode.AccessReject;
            }

            // copy initial request profile to challenge request context

            PacketCode response = await _multiFactorApiClient.Challenge(context, userAnswer, identifier);
            switch (response)
            {
                case PacketCode.AccessAccept:
                    var stateChallengePendingRequest = GetStateChallengeRequest(identifier);
                    stateChallengePendingRequest?.CopyProfileToContext(context);

                    if (stateChallengePendingRequest != null)
                    {
                        context.UserGroups = stateChallengePendingRequest.UserGroups;
                        context.ResponsePacket = stateChallengePendingRequest.ResponsePacket;
                        context.LdapAttrs = stateChallengePendingRequest.LdapAttrs;
                    }
                    break;
                case PacketCode.AccessReject:
                    RemoveStateChallengeRequest(identifier);
                    break;
            }

            return response;
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
                    context.RequestPacket.Identifier, context.ClientConfiguration.Name);
            }
        }

        /// <summary>
        /// Get authenticated request from local cache for otp/challenge
        /// </summary>
        private RadiusContext GetStateChallengeRequest(ChallengeRequestIdentifier identifier)
        {
            if (_stateChallengePendingRequests.TryRemove(identifier, out RadiusContext request))
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