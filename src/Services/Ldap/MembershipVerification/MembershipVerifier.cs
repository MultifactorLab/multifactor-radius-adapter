//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Radius.Adapter/blob/master/LICENSE.md

using LdapForNet;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification
{
    public class MembershipVerifier
    {
        private readonly ProfileLoader _profileLoader;
        private readonly ILogger _logger;

        public MembershipVerifier(ProfileLoader profileLoader, ILogger logger)
        {
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
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
                _logger.Warning("Can't find User-Name in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return result;
            }

            LdapProfile profile = null;
            //trying to authenticate for each domain/forest
            foreach (var domain in clientConfig.SplittedActiveDirectoryDomains)
            {
                var domainIdentity = LdapIdentity.FqdnToDn(domain);

                try
                {
                    var user = LdapIdentity.ParseUser(userName);

                    _logger.Debug($"Verifying user '{{user:l}}' membership at {domainIdentity}", user.Name);
                    using (var connAdapter = await LdapConnectionAdapter.CreateAsync(
                        domain, 
                        LdapIdentity.ParseUser(clientConfig.ServiceAccountUser), 
                        clientConfig.ServiceAccountPassword))
                    {
                        if (profile == null)
                        {
                            var dmn = await connAdapter.WhereAmIAsync();
                            profile = await _profileLoader.LoadAsync(clientConfig, connAdapter, dmn, user);
                        }
                        if (profile == null)
                        {
                            result.AddDomainResult(MembershipVerificationResult.Create(domainIdentity)
                                .SetSuccess(false)
                                .Build());
                            continue;
                        }

                        var res = VerifyMembership(clientConfig, profile, domainIdentity, user);
                        result.AddDomainResult(res);

                        if (res.IsSuccess) break;
                    }
                }
                catch (UserDomainNotPermittedException ex)
                {
                    _logger.Warning(ex.Message);
                    result.AddDomainResult(MembershipVerificationResult.Create(domainIdentity)
                        .SetSuccess(false)
                        .Build());
                    continue;
                }
                catch (UserNameFormatException ex)
                {
                    _logger.Warning(ex.Message);
                    result.AddDomainResult(MembershipVerificationResult.Create(domainIdentity)
                        .SetSuccess(false)
                        .Build());
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Verification user '{{user:l}}' membership at {domainIdentity} failed", userName);
                    _logger.Information("Run MultiFactor.Raduis.Adapter as user with domain read permissions (basically any domain user)");
                    result.AddDomainResult(MembershipVerificationResult.Create(domainIdentity)
                        .SetSuccess(false)
                        .Build());
                    continue;
                }
            }

            return result;
        }

        private MembershipVerificationResult VerifyMembership(ClientConfiguration clientConfig,
            LdapProfile profile,
            LdapIdentity domain,
            LdapIdentity user)
        {
            // user must be member of security group
            if (clientConfig.ActiveDirectoryGroup.Any())
            {
                var accessGroup = clientConfig.ActiveDirectoryGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                if (accessGroup != null)
                {
                    _logger.Debug($"User '{{user:l}}' is member of '{accessGroup.Trim()}' access group in {profile.BaseDn.Name}", user.Name);
                }
                else
                {
                    _logger.Warning($"User '{{user:l}}' is not member of '{string.Join(";", clientConfig.ActiveDirectoryGroup)}' access group in {profile.BaseDn.Name}", user.Name);
                    return MembershipVerificationResult.Create(domain)
                        .SetSuccess(false)
                        .SetProfile(profile)
                        .Build();
                }
            }

            var resBuilder = MembershipVerificationResult.Create(domain)
                        .SetSuccess(true)
                        .SetProfile(profile);

            //only users from group must process 2fa
            if (clientConfig.ActiveDirectory2FaGroup.Any())
            {
                var mfaGroup = clientConfig.ActiveDirectory2FaGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                if (mfaGroup != null)
                {
                    _logger.Debug($"User '{{user:l}}' is member of '{mfaGroup.Trim()}' 2FA group in {profile.BaseDn.Name}", user.Name);
                    resBuilder.SetIsMemberOf2FaGroups(true);
                }
                else
                {
                    _logger.Information($"User '{{user:l}}' is not member of '{string.Join(";", clientConfig.ActiveDirectory2FaGroup)}' 2FA group in {profile.BaseDn.Name}", user.Name);
                }
            }

            if (resBuilder.Subject.IsMemberOf2FaGroups && clientConfig.ActiveDirectory2FaBypassGroup.Any())
            {
                var bypassGroup = clientConfig.ActiveDirectory2FaBypassGroup.FirstOrDefault(group => IsMemberOf(profile, group));
                if (bypassGroup != null)
                {
                    _logger.Information($"User '{{user:l}}' is member of '{bypassGroup.Trim()}' 2FA bypass group in {profile.BaseDn.Name}", user.Name);
                    resBuilder.SetIsMemberOf2FaBypassGroup(true);
                }
                else
                {
                    _logger.Debug($"User '{{user:l}}' is not member of '{string.Join(";", clientConfig.ActiveDirectory2FaBypassGroup)}' 2FA bypass group in {profile.BaseDn.Name}", user.Name);
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