//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MultiFactor.Radius.Adapter.Server
{
    /// <summary>
    /// Main processor
    /// </summary>
    public class RadiusRouter
    {
        private IServiceConfiguration _serviceConfiguration;
        private ILogger _logger;
        private IRadiusPacketParser _packetParser;
        private MultiFactorApiClient _multifactorApiClient;
        private readonly FirstAuthFactorProcessorProvider _firstAuthFactorProcessorProvider;
        private readonly ChallengeProcessor _challengeProcessor;

        public event EventHandler<RadiusContext> RequestProcessed;

        private DateTime _startedAt = DateTime.Now;

        public RadiusRouter(IServiceConfiguration serviceConfiguration, 
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

        public async Task HandleRequest(RadiusContext context)
        {
            try
            {
                if (context.RequestPacket.Code == PacketCode.StatusServer)
                {
                    //status
                    var uptime = (DateTime.Now - _startedAt);
                    context.ReplyMessage = $"Server up {uptime.Days} days {uptime.ToString("hh\\:mm\\:ss")}";
                    context.ResponseCode = PacketCode.AccessAccept;
                    RequestProcessed?.Invoke(this, context);
                    return;
                }

                if (context.RequestPacket.Code != PacketCode.AccessRequest)
                {
                    _logger.Warning("Unprocessable packet type: {code}", context.RequestPacket.Code);
                    return;
                }

                ProcessUserNameTransformRules(context);

                if (context.RequestPacket.Attributes.ContainsKey("State")) //Access-Challenge response 
                {
                    var identifier = new ChallengeRequestIdentifier(context.ClientConfiguration, context.RequestPacket.GetString("State"));

                    if (_challengeProcessor.HasState(identifier))
                    {
                        //second request with Multifactor challenge
                        context.ResponseCode = await _challengeProcessor.ProcessChallenge(identifier, context);
                        context.State = identifier.RequestId;  //state for Access-Challenge message if otp is wrong (3 times allowed)

                        RequestProcessed?.Invoke(this, context);
                        return; //stop authentication process after otp code verification
                    }
                }

                var firstAuthProcessor = _firstAuthFactorProcessorProvider.GetProcessor(context.ClientConfiguration.FirstFactorAuthenticationSource);
                var firstFactorAuthenticationResultCode = await firstAuthProcessor.ProcessFirstAuthFactorAsync(context);
                if (firstFactorAuthenticationResultCode != PacketCode.AccessAccept)
                {
                    //first factor authentication rejected
                    context.ResponseCode = firstFactorAuthenticationResultCode;
                    RequestProcessed?.Invoke(this, context);

                    //stop authencation process
                    return;
                }

                if (context.Bypass2Fa)
                {
                    //second factor not required
                    var userName = context.UserName;
                    _logger.Information("Bypass second factor for user '{user:l}'", userName);

                    context.ResponseCode = PacketCode.AccessAccept;
                    RequestProcessed?.Invoke(this, context);

                    //stop authencation process
                    return;
                }

                context.ResponseCode = await ProcessSecondAuthenticationFactor(context);
                if (context.ResponseCode == PacketCode.AccessChallenge)
                {
                    _challengeProcessor.AddState(new ChallengeRequestIdentifier(context.ClientConfiguration, context.State), context);
                }

                RequestProcessed?.Invoke(this, context);

            }
            catch(Exception ex)
            {
                _logger.Error(ex, "HandleRequest");
            }
        }

        private void ProcessUserNameTransformRules(RadiusContext request)
        {
            var userName = request.UserName;
            if (string.IsNullOrEmpty(userName)) return;
            
            foreach(var rule in request.ClientConfiguration.UserNameTransformRules)
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
        private async Task<PacketCode> ProcessSecondAuthenticationFactor(RadiusContext request)
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
                if (request.ClientConfiguration.FirstFactorAuthenticationSource == AuthenticationSource.Radius)
                {
                    _logger.Information("Bypass second factor for user {user:l}", userName);
                    return PacketCode.AccessAccept;
                }
            }

            var response = await _multifactorApiClient.CreateSecondFactorRequest(request);

            return response;
        }
    }
}