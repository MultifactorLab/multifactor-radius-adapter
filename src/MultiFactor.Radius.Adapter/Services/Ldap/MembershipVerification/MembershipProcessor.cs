//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Radius.Adapter/blob/master/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification
{
    public class MembershipProcessor
    {
        private readonly ProfileLoader _profileLoader;
        private readonly MembershipVerifier _membershipVerifier;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public MembershipProcessor(ProfileLoader profileLoader,
            MembershipVerifier membershipVerifier,
            ILogger<MembershipProcessor> logger,
            ILoggerFactory loggerFactory)
        {
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
            _membershipVerifier = membershipVerifier ?? throw new ArgumentNullException(nameof(membershipVerifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Validate user membership within Active Directory Domain without password authentication
        /// </summary>
        public async Task<MembershipProcessingResult> ProcessMembershipAsync(RadiusContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var userName = UserNameTransformation.Transform(context.UserName, context.Configuration.UserNameTransformRules.BeforeFirstFactor);
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("Verification user' membership failed: can't find 'User-Name' attribute (messageId: {id}, from: {host:l}:{port})", context.RequestPacket.Header.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                return MembershipProcessingResult.Empty;
            }

            var results = new List<MembershipVerificationResult>();
            LdapProfile profile = null;
            var clientConfig = context.Configuration;
            var user = LdapIdentity.ParseUser(userName);
            var serviceUser = LdapIdentity.ParseUser(clientConfig.ServiceAccountUser);
            var formatter = new BindIdentityFormatter(clientConfig);
            //trying to authenticate for each domain/forest
            foreach (var domain in context.Configuration.SplittedActiveDirectoryDomains)
            {
                try
                {
                    _logger.LogDebug("Verifying user '{user:l}' membership at '{domain:l}'", user.Name, domain);
                    using var connAdapter = LdapConnectionAdapter.CreateAsTechnicalAccAsync(domain, clientConfig, _loggerFactory.CreateLogger<LdapConnectionAdapter>());

                    await connAdapter.BindAsync(formatter.FormatIdentity(serviceUser, domain), clientConfig.ServiceAccountPassword);
                    profile ??= await _profileLoader.LoadAsync(clientConfig, connAdapter, user);

                    var res = _membershipVerifier.VerifyMembership(clientConfig, profile, domain, user);
                    results.Add(res);

                    if (res.IsSuccess)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Verification user '{user:l}' membership at '{domain:l}' failed: {msg:l}", userName, domain, ex.Message);
                    results.Add(MembershipVerificationResult.Create(domain)
                        .SetSuccess(false)
                        .Build());
                }
            }

            return new MembershipProcessingResult(results);
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
            var transformedUserName = UserNameTransformation.Transform(context.UserName, context.Configuration.UserNameTransformRules.BeforeFirstFactor);
            if (string.IsNullOrEmpty(transformedUserName))
            {
                throw new Exception($"Can't find User-Name in message id={context.RequestPacket.Header.Identifier} from {context.RemoteEndpoint.Address}:{context.RemoteEndpoint.Port}");
            }
            var user = LdapIdentity.ParseUser(transformedUserName);
            var clientConfig = context.Configuration;
            var serviceUser = LdapIdentity.ParseUser(clientConfig.ServiceAccountUser);
            var formatter = new BindIdentityFormatter(context.Configuration);
            foreach (var domain in clientConfig.SplittedActiveDirectoryDomains)
            {
                var domainIdentity = LdapIdentity.FqdnToDn(domain);

                try
                {
                    using var connAdapter = LdapConnectionAdapter.CreateAsTechnicalAccAsync(domain, clientConfig, _loggerFactory.CreateLogger<LdapConnectionAdapter>());
                    await connAdapter.BindAsync(formatter.FormatIdentity(serviceUser, domain), context.Configuration.ServiceAccountPassword);
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
                }
            }

            return null;
        }
    }
}