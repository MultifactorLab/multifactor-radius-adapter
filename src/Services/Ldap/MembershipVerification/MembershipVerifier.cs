//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Radius.Adapter/blob/master/LICENSE.md

using LdapForNet;
using Microsoft.VisualBasic;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.BindIdentityFormatting;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection.Exceptions;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification
{
    public class MembershipVerifier
    {
        private readonly ProfileLoader _profileLoader;
        private readonly LdapConnectionAdapterFactory _connectionAdapterFactory;
        private readonly ILogger _logger;

        public MembershipVerifier(ProfileLoader profileLoader,
            LdapConnectionAdapterFactory connectionAdapterFactory,
            ILogger logger)
        {
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
            _connectionAdapterFactory = connectionAdapterFactory ?? throw new ArgumentNullException(nameof(connectionAdapterFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validate user membership within Active Directory Domain without password authentication
        /// </summary>
        public async Task<ComplexMembershipVerificationResult> VerifyMembershipAsync(PendingRequest request, ClientConfiguration clientConfig)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (clientConfig is null) throw new ArgumentNullException(nameof(clientConfig));

            var result = new ComplexMembershipVerificationResult();

            var userName = request.UserName;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning("Verification user' membership failed: can't find 'User-Name' attribute (messageId: {id}, from: {host:l}:{port})", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return result;
            }

            LdapProfile profile = null;
            //trying to authenticate for each domain/forest
            foreach (var domain in clientConfig.SplittedActiveDirectoryDomains)
            {
                try
                {
                    var user = LdapIdentity.ParseUser(userName);

                    _logger.Debug("Verifying user '{user:l}' membership at '{domain:l}'", user.Name, domain);
                    using (var connAdapter = await _connectionAdapterFactory.CreateAdapterAsTechnicalAccAsync(domain, clientConfig))
                    {
                        if (profile == null)
                        {
                            var dmn = await connAdapter.WhereAmIAsync();
                            profile = await _profileLoader.LoadAsync(clientConfig, connAdapter, dmn, user);
                        }

                        var res = VerifyMembership(clientConfig, profile, domain, user);
                        result.AddDomainResult(res);

                        if (res.IsSuccess) break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Verification user '{user:l}' membership at '{domain:l}' failed: {msg:l}", userName, domain, ex.Message);
                    result.AddDomainResult(MembershipVerificationResult.Create(domain)
                        .SetSuccess(false)
                        .Build());
                    continue;
                }
            }

            return result;
        }

        private MembershipVerificationResult VerifyMembership(ClientConfiguration clientConfig,
            LdapProfile profile,
            string domain,
            LdapIdentity user)
        {
            // user must be member of security group
            if (clientConfig.ActiveDirectoryGroup.Any())
            {
                var accessGroup = clientConfig.ActiveDirectoryGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                if (accessGroup != null)
                {
                    _logger.Debug("User '{user:l}' is a member of the access group '{group:l}' in '{domain:l}'", 
                        user.Name, accessGroup, profile.BaseDn.Name);
                }
                else
                {
                    _logger.Warning("User '{user:l}' is not a member of any access group ({accGroups:l}) in '{domain:l}'", 
                        user.Name, string.Join(", ", clientConfig.ActiveDirectoryGroup), profile.BaseDn.Name);
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
                    _logger.Debug("User '{user:l}' is a member of the 2FA group '{group:l}' in '{domain:l}'",
                        user.Name, mfaGroup.Trim(), profile.BaseDn.Name);
                    resBuilder.SetIsMemberOf2FaGroups(true);
                }
                else
                {
                    _logger.Information("User '{user:l}' is not a member of any 2FA group ({groups:l}) in '{domain:l}'", 
                        user.Name, string.Join(", ", clientConfig.ActiveDirectory2FaGroup), profile.BaseDn.Name);
                }
            }

            resBuilder.SetAre2FaBypassGroupsSpecified(clientConfig.ActiveDirectory2FaBypassGroup.Any());
            if (resBuilder.Subject.Are2FaBypassGroupsSpecified)
            {
                var bypassGroup = clientConfig.ActiveDirectory2FaBypassGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                if (bypassGroup != null)
                {
                    _logger.Information("User '{user:l}' is a member of the 2FA bypass group '{group:l}' in '{domain:l}'", 
                        user.Name, bypassGroup.Trim(), profile.BaseDn.Name);
                    resBuilder.SetIsMemberOf2FaBypassGroup(true);
                }
                else
                {
                    _logger.Debug("User '{user:l}' is not a member of any 2FA bypass group ({groups:l}) in '{domain:l}'", 
                        user.Name, string.Join(", ", clientConfig.ActiveDirectory2FaBypassGroup), profile.BaseDn.Name);
                }
            }

            return resBuilder.Build();
        }

        private bool IsMemberOf(LdapProfile profile, string group)
        {
            return profile.MemberOf?.Any(g => g.ToLower() == group.ToLower().Trim()) ?? false;
        }
    }
}