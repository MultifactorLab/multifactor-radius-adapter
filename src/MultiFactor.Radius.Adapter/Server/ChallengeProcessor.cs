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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server
{
    public class ChallengeProcessor
    {
        private readonly ConcurrentDictionary<ChallengeRequestIdentifier, RadiusContext> _stateChallengePendingRequests = new();

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
        public async Task<PacketCode> ProcessChallenge(ChallengeRequestIdentifier identifier, RadiusContext context)
        {
            var userName = context.UserName;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning("Can't find User-Name in message id={id} from {host:l}:{port}", context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            PacketCode response;
            string userAnswer;

            switch (context.RequestPacket.AuthenticationType)
            {
                case AuthenticationType.PAP:
                    //user-password attribute holds second request challenge from user
                    userAnswer = context.RequestPacket.GetString("User-Password");

                    if (string.IsNullOrEmpty(userAnswer))
                    {
                        _logger.Warning("Can't find User-Password with user response in message id={id} from {host:l}:{port}", context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                        return PacketCode.AccessReject;
                    }

                    break;
                case AuthenticationType.MSCHAP2:
                    var msChapResponse = context.RequestPacket.GetAttribute<byte[]>("MS-CHAP2-Response");

                    if (msChapResponse == null)
                    {
                        _logger.Warning("Can't find MS-CHAP2-Response in message id={id} from {host:l}:{port}", context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                        return PacketCode.AccessReject;
                    }

                    //forti behaviour
                    var otpData = msChapResponse.Skip(2).Take(6).ToArray();
                    userAnswer = Encoding.ASCII.GetString(otpData);

                    break;
                default:
                    _logger.Warning("Unable to process {auth} challange in message id={id} from {host:l}:{port}", context.RequestPacket.AuthenticationType, context.RequestPacket.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                    return PacketCode.AccessReject;
            }

            response = await _multiFactorApiClient.Challenge(context, userName, userAnswer, identifier);

            switch (response)
            {
                case PacketCode.AccessAccept:
                    var stateChallengePendingRequest = GetStateChallengeRequest(identifier);
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
                _logger.Error("Unable to cache request id={id} for the '{cfg:l}' configuration", 
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

            _logger.Error($"Unable to get cached request with state={identifier}");
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

    public class ChallengeRequestIdentifier
    {
        private readonly string _identifier;

        public string RequestId { get; }

        public ChallengeRequestIdentifier(IClientConfiguration client, string requestId)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (string.IsNullOrWhiteSpace(requestId))
            {
                throw new ArgumentException($"'{nameof(requestId)}' cannot be null or whitespace.", nameof(requestId));
            }

            _identifier = $"{client.Name}-{requestId}";
            RequestId = requestId;
        }

        public override string ToString()
        {
            return _identifier;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (obj == this) return true;

            var other = obj as ChallengeRequestIdentifier;
            if (other == null) return false;

            return _identifier == other._identifier;
        }

        public override int GetHashCode()
        {
            return 23 + _identifier.GetHashCode();
        }
    }
}