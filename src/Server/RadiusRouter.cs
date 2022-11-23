//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server
{
    /// <summary>
    /// Main processor
    /// </summary>
    public class RadiusRouter
    {
        private ServiceConfiguration _serviceConfiguration;
        private ILogger _logger;
        private IRadiusPacketParser _packetParser;
        private MultiFactorApiClient _multifactorApiClient;
        private readonly FirstAuthFactorProcessorProvider _firstAuthFactorProcessorProvider;
        private readonly ChallengeProcessor _challengeProcessor;

        public event EventHandler<PendingRequest> RequestProcessed;

        private DateTime _startedAt = DateTime.Now;

        public RadiusRouter(ServiceConfiguration serviceConfiguration, 
            IRadiusPacketParser packetParser,
            MultiFactorApiClient multifactorApiClient,
            FirstAuthFactorProcessorProvider firstAuthFactorProcessorProvider,
            ChallengeProcessor challengeProcessor,
            ILogger logger)
        {
            _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
            _packetParser = packetParser ?? throw new ArgumentNullException(nameof(packetParser));
            _multifactorApiClient = multifactorApiClient ?? throw new ArgumentNullException(nameof(multifactorApiClient));
            _firstAuthFactorProcessorProvider = firstAuthFactorProcessorProvider ?? throw new ArgumentNullException(nameof(firstAuthFactorProcessorProvider));
            _challengeProcessor = challengeProcessor ?? throw new ArgumentNullException(nameof(challengeProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleRequest(PendingRequest request, ClientConfiguration clientConfig)
        {
            try
            {
                if (request.RequestPacket.Code == PacketCode.StatusServer)
                {
                    //status
                    var uptime = (DateTime.Now - _startedAt);
                    request.ReplyMessage = $"Server up {uptime.Days} days {uptime.ToString("hh\\:mm\\:ss")}";
                    request.ResponseCode = PacketCode.AccessAccept;
                    RequestProcessed?.Invoke(this, request);
                    return;
                }

                if (request.RequestPacket.Code != PacketCode.AccessRequest)
                {
                    _logger.Warning("Unprocessable packet type: {code}", request.RequestPacket.Code);
                    return;
                }

                ProcessUserNameTransformRules(request, clientConfig);

                if (request.RequestPacket.Attributes.ContainsKey("State")) //Access-Challenge response 
                {
                    var receivedState = request.RequestPacket.GetString("State");

                    if (_challengeProcessor.HasState(receivedState))
                    {
                        //second request with Multifactor challenge
                        request.ResponseCode = await _challengeProcessor.ProcessChallenge(request, clientConfig, receivedState);
                        request.State = receivedState;  //state for Access-Challenge message if otp is wrong (3 times allowed)

                        RequestProcessed?.Invoke(this, request);
                        return; //stop authentication process after otp code verification
                    }
                }

                var firstAuthProcessor = _firstAuthFactorProcessorProvider.GetProcessor(clientConfig.FirstFactorAuthenticationSource);
                var firstFactorAuthenticationResultCode = await firstAuthProcessor.ProcessFirstAuthFactorAsync(request, clientConfig);
                if (firstFactorAuthenticationResultCode != PacketCode.AccessAccept)
                {
                    //first factor authentication rejected
                    request.ResponseCode = firstFactorAuthenticationResultCode;
                    RequestProcessed?.Invoke(this, request);

                    //stop authencation process
                    return;
                }

                if (request.Bypass2Fa)
                {
                    //second factor not required
                    var userName = request.UserName;
                    _logger.Information("Bypass second factor for user '{user:l}'", userName);

                    request.ResponseCode = PacketCode.AccessAccept;
                    RequestProcessed?.Invoke(this, request);

                    //stop authencation process
                    return;
                }

                request.ResponseCode = await ProcessSecondAuthenticationFactor(request, clientConfig);
                if (request.ResponseCode == PacketCode.AccessChallenge)
                {
                    _challengeProcessor.AddState(request.State, request);
                }

                RequestProcessed?.Invoke(this, request);

            }
            catch(Exception ex)
            {
                _logger.Error(ex, "HandleRequest");
            }
        }

        private void ProcessUserNameTransformRules(PendingRequest request, ClientConfiguration clientConfig)
        {
            var userName = request.UserName;
            if (string.IsNullOrEmpty(userName)) return;
            
            foreach(var rule in clientConfig.UserNameTransformRules)
            {
                var regex = new Regex(rule.Match);
                if (rule.Count != null)
                {
                    userName = regex.Replace(userName, rule.Replace, rule.Count.Value);
                }
                else
                {
                    userName = regex.Replace(userName, rule.Replace);
                }
            }

            request.UserName = userName;
        }

        /// <summary>
        /// Authenticate request at MultiFactor with user-name only
        /// </summary>
        private async Task<PacketCode> ProcessSecondAuthenticationFactor(PendingRequest request, ClientConfiguration clientConfig)
        {
            var userName = request.UserName;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning("Can't find User-Name in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            if (request.RequestPacket.IsVendorAclRequest == true)
            {
                //security check
                if (clientConfig.FirstFactorAuthenticationSource == AuthenticationSource.Radius)
                {
                    _logger.Information("Bypass second factor for user {user:l}", userName);
                    return PacketCode.AccessAccept;
                }
            }

            var response = await _multifactorApiClient.CreateSecondFactorRequest(request, clientConfig);

            return response;
        }
    }
}