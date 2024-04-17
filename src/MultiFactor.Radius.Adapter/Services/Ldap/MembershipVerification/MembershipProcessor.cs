//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Radius.Adapter/blob/master/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification
{
    public class MembershipProcessor
    {
        private readonly ProfileLoader _profileLoader;
        private readonly LdapConnectionAdapterFactory _connectionAdapterFactory;
        private readonly MembershipVerifier _membershipVerifier;
        private readonly ILogger _logger;

        public MembershipProcessor(ProfileLoader profileLoader,
            LdapConnectionAdapterFactory connectionAdapterFactory,
            MembershipVerifier membershipVerifier,
            ILogger<MembershipProcessor> logger)
        {
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
            _connectionAdapterFactory = connectionAdapterFactory ?? throw new ArgumentNullException(nameof(connectionAdapterFactory));
            _membershipVerifier = membershipVerifier ?? throw new ArgumentNullException(nameof(membershipVerifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validate user membership within Active Directory Domain without password authentication
        /// </summary>
        public async Task<IMembershipProcessingResult> ProcessMembershipAsync(RadiusContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var result = new MembershipProcessingResult();

            var userName = context.UserName;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("Verification user' membership failed: can't find 'User-Name' attribute (messageId: {id}, from: {host:l}:{port})", context.RequestPacket.Header.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                return result;
            }

            LdapProfile profile = null;
            //trying to authenticate for each domain/forest
            foreach (var domain in context.Configuration.SplittedActiveDirectoryDomains)
            {
                try
                {
                    var user = LdapIdentity.ParseUser(userName);

                    _logger.LogDebug("Verifying user '{user:l}' membership at '{domain:l}'", user.Name, domain);
                    using var connAdapter = await _connectionAdapterFactory.CreateAdapterAsTechnicalAccAsync(domain, context.Configuration);
                    profile ??= await _profileLoader.LoadAsync(context.Configuration, connAdapter, user);

                    var res = _membershipVerifier.VerifyMembership(context.Configuration, profile, domain, user);
                    result.AddDomainResult(res);

                    if (res.IsSuccess)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Verification user '{user:l}' membership at '{domain:l}' failed: {msg:l}", userName, domain, ex.Message);
                    result.AddDomainResult(MembershipVerificationResult.Create(domain)
                        .SetSuccess(false)
                        .Build());
                    continue;
                }
            }

            return result;
        }

        /// <summary>
        /// Load required attribute and set it in user profile
        /// </summary>
        public async Task<LdapProfile> LoadProfileWithRequiredAttributeAsync(RadiusContext context, string attr)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(context.UserName))
            {
                throw new Exception($"Can't find User-Name in message id={context.RequestPacket.Header.Identifier} from {context.RemoteEndpoint.Address}:{context.RemoteEndpoint.Port}");
            }
            var user = LdapIdentity.ParseUser(context.UserName);

            var clientConfig = context.Configuration;
            foreach (var domain in clientConfig.SplittedActiveDirectoryDomains)
            {
                var domainIdentity = LdapIdentity.FqdnToDn(domain);

                try
                {
                    using var connAdapter = await _connectionAdapterFactory.CreateAdapterAsTechnicalAccAsync(domain, clientConfig);
                    var attributes = await _profileLoader.LoadAttributesAsync(clientConfig, connAdapter, user, new[] { attr });
                    if (attributes.Keys.Count == 0)
                    {
                        continue;
                    }

                    var profile = new LdapProfile(domainIdentity,
                        attributes,
                        clientConfig.PhoneAttributes,
                        clientConfig.TwoFAIdentityAttribute);
                    return profile;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Loading attributes of user '{user:l}' at {domainIdentity} failed", context.UserName, domainIdentity);
                    _logger.LogInformation("Run MultiFactor.Raduis.Adapter as user with domain read permissions (basically any domain user)");
                    continue;
                }
            }

            return null;
        }
    }
}