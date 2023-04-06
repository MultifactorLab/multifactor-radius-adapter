//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Services.ActiveDirectory.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using Serilog;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing
{
    /// <summary>
    /// Authenticate request at Remote Radius Server with user-name and password
    /// </summary>
    public class RadiusFirstAuthFactorProcessor : IFirstAuthFactorProcessor
    {
        private readonly MembershipProcessor _membershipProcessor;
        private readonly IRadiusPacketParser _packetParser;
        private readonly ILogger _logger;

        public RadiusFirstAuthFactorProcessor(MembershipProcessor membershipProcessor,
            IRadiusPacketParser packetParser,
            ILogger logger)
        {
            _membershipProcessor = membershipProcessor ?? throw new ArgumentNullException(nameof(membershipProcessor));
            _packetParser = packetParser ?? throw new ArgumentNullException(nameof(packetParser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public AuthenticationSource AuthenticationSource => AuthenticationSource.Radius;

        public async Task<PacketCode> ProcessFirstAuthFactorAsync(PendingRequest request, IClientConfiguration clientConfig)
        {
            var code = await ProcessRadiusAuthAsync(request, clientConfig);
            if (code != PacketCode.AccessAccept) return code;

            if (!clientConfig.CheckMembership) return PacketCode.AccessAccept;

            var result = await _membershipProcessor.ProcessMembershipAsync(request, clientConfig);
            var handler = new MembershipProcessingResultHandler(result);

            handler.EnrichRequest(request);
            return handler.GetDecision();
        }

        private async Task<PacketCode> ProcessRadiusAuthAsync(PendingRequest request, IClientConfiguration clientConfig)
        {
            try
            {
                //sending request as is to Remote Radius Server
                using (var client = new RadiusClient(clientConfig.ServiceClientEndpoint, _logger))
                {
                    _logger.Debug($"Sending AccessRequest message with id={{id}} to Remote Radius Server {clientConfig.NpsServerEndpoint}", request.RequestPacket.Identifier);

                    var requestBytes = _packetParser.GetBytes(request.RequestPacket);
                    var response = await client.SendPacketAsync(request.RequestPacket.Identifier, requestBytes, clientConfig.NpsServerEndpoint, TimeSpan.FromSeconds(5));

                    if (response != null)
                    {
                        var responsePacket = _packetParser.Parse(response, request.RequestPacket.SharedSecret, request.RequestPacket.Authenticator);
                        _logger.Debug("Received {code:l} message with id={id} from Remote Radius Server", responsePacket.Code.ToString(), responsePacket.Identifier);

                        if (responsePacket.Code == PacketCode.AccessAccept)
                        {
                            var userName = request.UserName;
                            _logger.Information($"User '{{user:l}}' credential and status verified successfully at {clientConfig.NpsServerEndpoint}", userName);
                        }

                        request.ResponsePacket = responsePacket;
                        return responsePacket.Code; //Code received from remote radius
                    }
                    else
                    {
                        _logger.Warning("Remote Radius Server did not respond on message with id={id}", request.RequestPacket.Identifier);
                        return PacketCode.AccessReject; //reject by default
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Radius authentication error");
            }

            return PacketCode.AccessReject; //reject by default
        }
    }
}