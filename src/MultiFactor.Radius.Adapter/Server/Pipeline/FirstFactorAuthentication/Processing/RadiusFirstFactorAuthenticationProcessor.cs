//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Services.ActiveDirectory.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.Processing
{
    /// <summary>
    /// Authenticate request at Remote Radius Server with user-name and password
    /// </summary>
    public class RadiusFirstFactorAuthenticationProcessor : IFirstFactorAuthenticationProcessor
    {
        private readonly MembershipProcessor _membershipProcessor;
        private readonly RadiusPacketParser _packetParser;
        private readonly ILogger<RadiusFirstFactorAuthenticationProcessor> _logger;

        public RadiusFirstFactorAuthenticationProcessor(MembershipProcessor membershipProcessor,
            RadiusPacketParser packetParser,
            ILogger<RadiusFirstFactorAuthenticationProcessor> logger)
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

            if (context.Configuration.CheckMembership)
            {
                var result = await _membershipProcessor.ProcessMembershipAsync(context);
                var handler = new MembershipProcessingResultHandler(result);

                handler.EnrichContext(context);
                return handler.GetDecision();
            }

            if (context.Configuration.UseIdentityAttribute)
            {
                var profile = await _membershipProcessor.LoadProfileWithRequiredAttributeAsync(context, context.Configuration.TwoFAIdentityAttribute);
                if (profile == null)
                {
                    _logger.LogWarning("Attribute '{TwoFAIdentityAttribyte}' was not loaded", context.Configuration.TwoFAIdentityAttribute);
                    return PacketCode.AccessReject;
                }

                profile.SetIdentityAttribute(context.Configuration.TwoFAIdentityAttribute);
                context.UpdateProfile(profile);
            }

            return PacketCode.AccessAccept;
        }

        private async Task<PacketCode> ProcessRadiusAuthAsync(RadiusContext context)
        {
            try
            {
                //sending request as is to Remote Radius Server
                using var client = new RadiusClient(context.Configuration.ServiceClientEndpoint, _logger);
                _logger.LogDebug("Sending AccessRequest message with id={id} to Remote Radius Server {endpoint:l}", context.RequestPacket.Header.Identifier, context.Configuration.NpsServerEndpoint);
                
                var identity = UserNameTransformation.Transform(context.UserName, context.Configuration.UserNameTransformRules.BeforeFirstFactor);
                var requestPacket = context.RequestPacket.Clone().UpdateAttribute("User-Name", identity);
                if (!string.IsNullOrWhiteSpace(context.Passphrase.Password))
                {
                    requestPacket.UpdateAttribute("User-Password", context.Passphrase.Password);
                }

                var requestBytes = _packetParser.GetBytes(requestPacket);
                var response = await client.SendPacketAsync(requestPacket.Header.Identifier, requestBytes, context.Configuration.NpsServerEndpoint, TimeSpan.FromSeconds(5));

                if (response != null)
                {
                    var responsePacket = _packetParser.Parse(response, requestPacket.SharedSecret, requestPacket.Header.Authenticator);
                    _logger.LogDebug("Received {code:l} message with id={id} from Remote Radius Server", responsePacket.Header.Code.ToString(), responsePacket.Header.Identifier);

                    if (responsePacket.Header.Code == PacketCode.AccessAccept)
                    {
                        var userName = context.UserName;
                        _logger.LogInformation("User '{user:l}' credential and status verified successfully at {endpoint:l}", userName, context.Configuration.NpsServerEndpoint);
                    }

                    context.ResponsePacket = responsePacket;

                    return responsePacket.Header.Code; //Code received from remote radius
                }
                else
                {
                    _logger.LogWarning("Remote Radius Server did not respond on message with id={id}", requestPacket.Header.Identifier);
                    context.Flags.SkipResponse();
                    return PacketCode.AccessReject;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Radius authentication error");
            }

            return PacketCode.AccessReject; //reject by default
        }
    }
}