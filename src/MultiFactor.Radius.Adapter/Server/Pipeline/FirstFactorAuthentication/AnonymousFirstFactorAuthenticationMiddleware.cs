using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Framework.Pipeline;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.FirstAuthFactorProcessing;
using MultiFactor.Radius.Adapter.Services.ActiveDirectory.MembershipVerification;
using MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication
{
    /// <summary>
    /// Verify user profile and membership without AD authentication.
    /// </summary>
    public class AnonymousFirstFactorAuthenticationMiddleware : IRadiusMiddleware
    {
        private readonly MembershipProcessor _membershipProcessor;
        private readonly ILogger<AnonymousFirstFactorAuthenticationMiddleware> _logger;

        public AnonymousFirstFactorAuthenticationMiddleware(MembershipProcessor membershipProcessor, 
            ILogger<AnonymousFirstFactorAuthenticationMiddleware> logger)
        {
            _membershipProcessor = membershipProcessor;
            _logger = logger;
        }

        public async Task InvokeAsync(RadiusContext context, RadiusRequestDelegate next)
        {
            var isNoneFirstFactorSource = context.FirstFactorAuthenticationSource == Configuration.AuthenticationSource.None;
            var preAuthModeEnabled = context.PreAuthMode != PreAuthMode.None;
            var needFirstFactorAuth = context.Authentication.FirstFactor == AuthenticationCode.Awaiting;
            var needSecondFactorAuth = context.Authentication.SecondFactor == AuthenticationCode.Awaiting;

            var shoulBeInvoked =
                (isNoneFirstFactorSource && needFirstFactorAuth)
                ||
                (preAuthModeEnabled && needSecondFactorAuth);

            if (!shoulBeInvoked)
            {
                await next(context);
                return;
            }

            if (context.Configuration.CheckMembership)
            {
                var result = await _membershipProcessor.ProcessMembershipAsync(context);
                var handler = new MembershipProcessingResultHandler(result);

                handler.EnrichContext(context);
                var code = handler.GetDecision();

                if (code != PacketCode.AccessAccept)
                {
                    _logger.LogError("Failed to validate user profile.");

                    context.SetFirstFactorAuth(AuthenticationCode.Reject);
                    return;
                }
            }

            if (context.Configuration.UseIdentityAttribute)
            {
                var profile = await _membershipProcessor.LoadProfileWithRequiredAttributeAsync(context, context.Configuration, context.Configuration.TwoFAIdentityAttribute);
                if (profile == null)
                {
                    _logger.LogWarning("User profile and attribute '{TwoFAIdentityAttribyte}' was not loaded", context.Configuration.TwoFAIdentityAttribute);
                    _logger.LogError("Failed to validate user profile.");

                    context.SetFirstFactorAuth(AuthenticationCode.Reject);
                    return;
                }
                context.UpdateProfile(profile);
            }

            if (isNoneFirstFactorSource)
            {
                context.SetFirstFactorAuth(AuthenticationCode.Accept);
            }

            await next(context);
        }
    }
}
