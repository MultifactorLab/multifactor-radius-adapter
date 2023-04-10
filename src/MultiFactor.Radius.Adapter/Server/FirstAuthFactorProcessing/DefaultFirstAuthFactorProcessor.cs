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
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing
{
    public class DefaultFirstAuthFactorProcessor : IFirstAuthFactorProcessor
    {
        private readonly MembershipProcessor _membershipProcessor;
        private readonly ILogger _logger;

        public DefaultFirstAuthFactorProcessor(MembershipProcessor membershipProcessor, ILogger logger)
        {
            _membershipProcessor = membershipProcessor ?? throw new System.ArgumentNullException(nameof(membershipProcessor));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public AuthenticationSource AuthenticationSource => AuthenticationSource.None;

        public async Task<PacketCode> ProcessFirstAuthFactorAsync(RadiusContext context)
        {
            if (!context.ClientConfiguration.CheckMembership)
            {
                return PacketCode.AccessAccept;
            }

            // check membership without AD authentication
            var result = await _membershipProcessor.ProcessMembershipAsync(context);
            var handler = new MembershipProcessingResultHandler(result);

            handler.EnrichRequest(context);
            return handler.GetDecision();
        }
    }
}