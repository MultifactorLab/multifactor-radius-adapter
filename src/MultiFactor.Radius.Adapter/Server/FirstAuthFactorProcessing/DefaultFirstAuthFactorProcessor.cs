//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Services.ActiveDirectory.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing
{
    public class DefaultFirstAuthFactorProcessor : IFirstAuthFactorProcessor
    {
        private readonly MembershipProcessor _membershipProcessor;
        private readonly ILogger _logger;

        public DefaultFirstAuthFactorProcessor(MembershipProcessor membershipProcessor, ILogger<DefaultFirstAuthFactorProcessor> logger)
        {
            _membershipProcessor = membershipProcessor ?? throw new System.ArgumentNullException(nameof(membershipProcessor));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public AuthenticationSource AuthenticationSource => AuthenticationSource.None;

        public async Task<PacketCode> ProcessFirstAuthFactorAsync(RadiusContext context)
        {
            if (context.ClientConfiguration.CheckMembership)
            {
                // check membership without AD authentication
                var result = await _membershipProcessor.ProcessMembershipAsync(context);
                var handler = new MembershipProcessingResultHandler(result);

                handler.EnrichRequest(context);
                return handler.GetDecision();
            }

            var packetCode = PacketCode.AccessAccept;
            packetCode = await CheckUpnIdentity(context);
            packetCode = await CheckAttributeIdentity(context);

            return packetCode;
        }

        // try get 'upn' attribute for second factor identity if required; reject request if attribute not present
        private async Task<PacketCode> CheckUpnIdentity(RadiusContext context)
        {
            if (!context.ClientConfiguration.UseUpnAsIdentity)
            {
                return PacketCode.AccessAccept;
            }
            var profile = await _membershipProcessor.LoadProfileWithRequiredAttributesAsync(context, context.ClientConfiguration, "userPrincipalName", (profile, attr) => profile.SetUpn(attr));
            if (profile == null)
            {
                _logger.LogWarning("Attribute 'userPrincipalName' was not loaded");
                return PacketCode.AccessReject;
            }
            context.SetProfile(profile);
            return PacketCode.AccessAccept;
        }

        // try get custom attribute for second factor identity if required; reject request if attribute not present
        private async Task<PacketCode> CheckAttributeIdentity(RadiusContext context)
        {
            if (context.ClientConfiguration.TwoFAIdentityAttribyte == null)
            {
                return PacketCode.AccessAccept;
            }
            var profile = await _membershipProcessor.LoadProfileWithRequiredAttributesAsync(context, context.ClientConfiguration, context.ClientConfiguration.TwoFAIdentityAttribyte, (profile, attr) => profile.SetIdentityAttribute(attr));
            if (profile == null)
            {
                _logger.LogWarning("Attribute '{TwoFAIdentityAttribyte}' was not loaded", context.ClientConfiguration.TwoFAIdentityAttribyte);
                return PacketCode.AccessReject;
            }
            context.SetProfile(profile);
            return PacketCode.AccessAccept;
        }
    }
}