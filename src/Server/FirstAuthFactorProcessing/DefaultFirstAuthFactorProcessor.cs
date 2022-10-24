//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Services.ActiveDirectory.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using Serilog;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing
{
    public class DefaultFirstAuthFactorProcessor : IFirstAuthFactorProcessor
    {
        private readonly MembershipVerifier _membershipVerifier;
        private readonly ILogger _logger;

        public DefaultFirstAuthFactorProcessor(MembershipVerifier membershipVerifier, ILogger logger)
        {
            _membershipVerifier = membershipVerifier ?? throw new System.ArgumentNullException(nameof(membershipVerifier));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public AuthenticationSource AuthenticationSource => AuthenticationSource.None;

        public async Task<PacketCode> ProcessFirstAuthFactorAsync(PendingRequest request, ClientConfiguration clientConfig)
        {
            if (!clientConfig.CheckMembership)
            {
                return PacketCode.AccessAccept;
            }

            // check membership without AD authentication
            var result = await _membershipVerifier.VerifyMembershipAsync(request, clientConfig);
            var handler = new MembershipVerificationResultHandler(result);

            handler.EnrichRequest(request);
            return handler.GetDecision();
        }
    }
}