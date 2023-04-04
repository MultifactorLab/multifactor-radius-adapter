//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server
{
    public class ChallengeProcessor
    {
        private readonly ConcurrentDictionary<string, PendingRequest> _stateChallengePendingRequests = 
            new ConcurrentDictionary<string, PendingRequest>();

        private readonly MultiFactorApiClient _multiFactorApiClient;
        private readonly ILogger _logger;

        public ChallengeProcessor(MultiFactorApiClient multiFactorApiClient, ILogger logger)
        {
            _multiFactorApiClient = multiFactorApiClient ?? throw new ArgumentNullException(nameof(multiFactorApiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verify one time password from user input
        /// </summary>
        public async Task<PacketCode> ProcessChallenge(PendingRequest request, IClientConfiguration clientConfig, string state)
        {
            var userName = request.UserName;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning("Can't find User-Name in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            PacketCode response;
            string userAnswer;

            switch (request.RequestPacket.AuthenticationType)
            {
                case AuthenticationType.PAP:
                    //user-password attribute holds second request challenge from user
                    userAnswer = request.RequestPacket.GetString("User-Password");

                    if (string.IsNullOrEmpty(userAnswer))
                    {
                        _logger.Warning("Can't find User-Password with user response in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                        return PacketCode.AccessReject;
                    }

                    break;
                case AuthenticationType.MSCHAP2:
                    var msChapResponse = request.RequestPacket.GetAttribute<byte[]>("MS-CHAP2-Response");

                    if (msChapResponse == null)
                    {
                        _logger.Warning("Can't find MS-CHAP2-Response in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                        return PacketCode.AccessReject;
                    }

                    //forti behaviour
                    var otpData = msChapResponse.Skip(2).Take(6).ToArray();
                    userAnswer = Encoding.ASCII.GetString(otpData);

                    break;
                default:
                    _logger.Warning("Unable to process {auth} challange in message id={id} from {host:l}:{port}", request.RequestPacket.AuthenticationType, request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                    return PacketCode.AccessReject;
            }

            response = await _multiFactorApiClient.Challenge(request, clientConfig, userName, userAnswer, state);

            switch (response)
            {
                case PacketCode.AccessAccept:
                    var stateChallengePendingRequest = GetStateChallengeRequest(state);
                    if (stateChallengePendingRequest != null)
                    {
                        request.UserGroups = stateChallengePendingRequest.UserGroups;
                        request.ResponsePacket = stateChallengePendingRequest.ResponsePacket;
                        request.LdapAttrs = stateChallengePendingRequest.LdapAttrs;
                    }
                    break;
                case PacketCode.AccessReject:
                    RemoveStateChallengeRequest(state);
                    break;
            }

            return response;
        }

        public bool HasState(string challengePendingRequestState)
        {
            return _stateChallengePendingRequests.ContainsKey(challengePendingRequestState);
        }

        /// <summary>
        /// Add authenticated request to local cache for otp/challenge
        /// </summary>
        public void AddState(string state, PendingRequest request)
        {
            if (!_stateChallengePendingRequests.TryAdd(state, request))
            {
                _logger.Error("Unable to cache request id={id}", request.RequestPacket.Identifier);
            }
        }

        /// <summary>
        /// Get authenticated request from local cache for otp/challenge
        /// </summary>
        private PendingRequest GetStateChallengeRequest(string state)
        {
            if (_stateChallengePendingRequests.TryRemove(state, out PendingRequest request))
            {
                return request;
            }

            _logger.Error($"Unable to get cached request with state={state}");
            return null;
        }

        /// <summary>
        /// Remove authenticated request from local cache
        /// </summary>
        /// <param name="state"></param>
        private void RemoveStateChallengeRequest(string state)
        {
            _stateChallengePendingRequests.TryRemove(state, out PendingRequest _);
        }
    }
}