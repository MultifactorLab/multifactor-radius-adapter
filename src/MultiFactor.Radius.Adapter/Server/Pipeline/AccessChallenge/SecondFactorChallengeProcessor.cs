//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge
{
    public class SecondFactorChallengeProcessor : IChallengeProcessor
    {
        private readonly ConcurrentDictionary<ChallengeIdentifier, RadiusContext> _challengeContexts = new();
        private readonly IMultifactorApiAdapter _apiAdapter;
        private readonly ILogger<SecondFactorChallengeProcessor> _logger;
        public ChallengeType ChallengeType => ChallengeType.SecondFactor;
        
        public SecondFactorChallengeProcessor(IMultifactorApiAdapter apiAdapter, ILogger<SecondFactorChallengeProcessor> logger)
        {
            _apiAdapter = apiAdapter ?? throw new ArgumentNullException(nameof(apiAdapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verify one time password from user input
        /// </summary>
        public async Task<ChallengeCode> ProcessChallengeAsync(ChallengeIdentifier identifier, RadiusContext context)
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
            var challengeContext = GetChallengeContext(identifier)
                ?? throw new InvalidOperationException($"Challenge context with identifier '{identifier}' was not found");
            var response = await _apiAdapter.ChallengeAsync(challengeContext, userAnswer, identifier);
            context.SetReplyMessage(response.ReplyMessage);
            switch (response.Code)
            {
                case AuthenticationCode.Accept:
                    if (challengeContext != null)
                    {
                        context.Update(challengeContext);
                    }

                    RemoveChallengeContext(identifier);
                    _logger.LogDebug("Challenge {State:l} was processed for message id={id} from {host:l}:{port} with result '{Result}'",
                        identifier.RequestId,
                        context.Header.Identifier,
                        context.RemoteEndpoint.Address,
                        context.RemoteEndpoint.Port, 
                        response.Code);
                    return ChallengeCode.Accept;

                case AuthenticationCode.Reject:
                    RemoveChallengeContext(identifier);
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

        public bool HasChallengeContext(ChallengeIdentifier identifier)
        {
            return _challengeContexts.ContainsKey(identifier);
        }

        /// <summary>
        /// Add authenticated request to local cache for otp/challenge
        /// </summary>
        public ChallengeIdentifier AddChallengeContext(RadiusContext context)
        {
            var id = new ChallengeIdentifier(context.Configuration.Name, context.State);
            if (_challengeContexts.TryAdd(id, context))
            {
                _logger.LogInformation("Challenge {State:l} was added for message id={id}", 
                    id.RequestId, context.Header.Identifier);
                return id;
            }

            _logger.LogError("Unable to cache request id={id} for the '{cfg:l}' configuration",
                context.Header.Identifier, context.Configuration.Name);
            return ChallengeIdentifier.Empty;
        }

        /// <summary>
        /// Get authenticated request from local cache for otp/challenge
        /// </summary>
        private RadiusContext GetChallengeContext(ChallengeIdentifier identifier)
        {
            if (_challengeContexts.TryGetValue(identifier, out RadiusContext request))
            {
                return request;
            }

            _logger.LogError("Unable to get cached request with state={identifier:l}", identifier);
            return null;
        }

        /// <summary>
        /// Remove authenticated request from local cache
        /// </summary>
        /// <param name="state"></param>
        private void RemoveChallengeContext(ChallengeIdentifier identifier)
        {
            _challengeContexts.TryRemove(identifier, out RadiusContext _);
        }
    }
}