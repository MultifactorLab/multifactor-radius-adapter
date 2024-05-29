//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Radius.Adapter/blob/master/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using System;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification
{
    public class MembershipVerifier
    {
        private readonly ILogger _logger;

        public MembershipVerifier(ILogger<MembershipVerifier> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public MembershipVerificationResult VerifyMembership(IClientConfiguration clientConfig, LdapProfile profile, string domain, LdapIdentity user)
        {
            if (clientConfig is null) throw new ArgumentNullException(nameof(clientConfig));
            if (profile is null) throw new ArgumentNullException(nameof(profile));
            if (string.IsNullOrEmpty(domain)) throw new ArgumentException($"'{nameof(domain)}' cannot be null or empty.", nameof(domain));
            if (user is null) throw new ArgumentNullException(nameof(user));
            
            // user must be member of security group
            if (clientConfig.ActiveDirectoryGroups.Any())
            {
                var accessGroup = clientConfig.ActiveDirectoryGroups.FirstOrDefault(group => IsMemberOf(profile, group));
                if (accessGroup != null)
                {
                    _logger.LogDebug("User '{user:l}' is a member of the access group '{group:l}' in '{domain:l}'",
                        user.Name, accessGroup, profile.BaseDn.Name);
                }
                else
                {
                    _logger.LogWarning("User '{user:l}' is not a member of any access group ({accGroups:l}) in '{domain:l}'",
                        user.Name, string.Join(", ", clientConfig.ActiveDirectoryGroups), profile.BaseDn.Name);
                    return MembershipVerificationResult.Create(domain)
                        .SetSuccess(false)
                        .SetProfile(profile)
                        .Build();
                }
            }

            var resBuilder = MembershipVerificationResult.Create(domain)
                        .SetSuccess(true)
                        .SetProfile(profile);

            resBuilder.SetAre2FaGroupsSpecified(clientConfig.ActiveDirectory2FaGroup.Any());
            if (resBuilder.Subject.Are2FaGroupsSpecified)
            {
                var mfaGroup = clientConfig.ActiveDirectory2FaGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                if (mfaGroup != null)
                {
                    _logger.LogDebug("User '{user:l}' is a member of the 2FA group '{group:l}' in '{domain:l}'",
                        user.Name, mfaGroup.Trim(), profile.BaseDn.Name);
                    resBuilder.SetIsMemberOf2FaGroups(true);
                }
                else
                {
                    _logger.LogInformation("User '{user:l}' is not a member of any 2FA group ({groups:l}) in '{domain:l}'",
                        user.Name, string.Join(", ", clientConfig.ActiveDirectory2FaGroup), profile.BaseDn.Name);
                }
            }

            resBuilder.SetAre2FaBypassGroupsSpecified(clientConfig.ActiveDirectory2FaBypassGroup.Any());
            if (resBuilder.Subject.Are2FaBypassGroupsSpecified)
            {
                var bypassGroup = clientConfig.ActiveDirectory2FaBypassGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                if (bypassGroup != null)
                {
                    _logger.LogInformation("User '{user:l}' is a member of the 2FA bypass group '{group:l}' in '{domain:l}'",
                        user.Name, bypassGroup.Trim(), profile.BaseDn.Name);
                    resBuilder.SetIsMemberOf2FaBypassGroup(true);
                }
                else
                {
                    _logger.LogDebug("User '{user:l}' is not a member of any 2FA bypass group ({groups:l}) in '{domain:l}'",
                        user.Name, string.Join(", ", clientConfig.ActiveDirectory2FaBypassGroup), profile.BaseDn.Name);
                }
            }

            return resBuilder.Build();
        }

        private bool IsMemberOf(LdapProfile profile, string group)
        {
            return profile.MemberOf.Any(g => g.ToLower() == group.ToLower().Trim());
        }
    }
}