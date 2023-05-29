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

        public async Task<PacketCode> ProcessFirstAuthFactorAsync(RadiusContext context)
        {
            var code = await ProcessRadiusAuthAsync(context);
            if (code != PacketCode.AccessAccept) return code;

            if (!context.ClientConfiguration.CheckMembership) return PacketCode.AccessAccept;

            var result = await _membershipProcessor.ProcessMembershipAsync(context);
            var handler = new MembershipProcessingResultHandler(result);

            handler.EnrichRequest(context);
            return handler.GetDecision();
        }

        private async Task<PacketCode> ProcessRadiusAuthAsync(RadiusContext context)
        {
            try
            {
                //sending request as is to Remote Radius Server
                using (var client = new RadiusClient(context.ClientConfiguration.ServiceClientEndpoint, _logger))
                {
                    _logger.Debug($"Sending AccessRequest message with id={{id}} to Remote Radius Server {context.ClientConfiguration.NpsServerEndpoint}", context.RequestPacket.Identifier);

                    var requestBytes = _packetParser.GetBytes(context.RequestPacket);
                    var response = await client.SendPacketAsync(context.RequestPacket.Identifier, requestBytes, context.ClientConfiguration.NpsServerEndpoint, TimeSpan.FromSeconds(5));

                    if (response != null)
                    {
                        var responsePacket = _packetParser.Parse(response, context.RequestPacket.SharedSecret, context.RequestPacket.Authenticator);
                        _logger.Debug("Received {code:l} message with id={id} from Remote Radius Server", responsePacket.Code.ToString(), responsePacket.Identifier);

                        if (responsePacket.Code == PacketCode.AccessAccept)
                        {
                            var userName = context.UserName;
                            _logger.Information($"User '{{user:l}}' credential and status verified successfully at {context.ClientConfiguration.NpsServerEndpoint}", userName);
                        }

                        context.ResponsePacket = responsePacket;
                        return responsePacket.Code; //Code received from remote radius
                    }
                    else
                    {
                        _logger.Warning("Remote Radius Server did not respond on message with id={id}", context.RequestPacket.Identifier);
                        return PacketCode.DisconnectNak; //reject by default
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