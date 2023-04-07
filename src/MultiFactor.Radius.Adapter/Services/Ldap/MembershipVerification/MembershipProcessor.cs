//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Radius.Adapter/blob/master/LICENSE.md

using LdapForNet;
using Microsoft.VisualBasic;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.BindIdentityFormatting;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection.Exceptions;
using MultiFactor.Radius.Adapter.Services.Ldap.ProfileLoading;
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
    public class MembershipProcessor
    {
        private readonly ProfileLoader _profileLoader;
        private readonly LdapConnectionAdapterFactory _connectionAdapterFactory;
        private readonly MembershipVerifier _membershipVerifier;
        private readonly ILogger _logger;

        public MembershipProcessor(ProfileLoader profileLoader,
            LdapConnectionAdapterFactory connectionAdapterFactory,
            MembershipVerifier membershipVerifier,
            ILogger logger)
        {
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
            _connectionAdapterFactory = connectionAdapterFactory ?? throw new ArgumentNullException(nameof(connectionAdapterFactory));
            _membershipVerifier = membershipVerifier ?? throw new ArgumentNullException(nameof(membershipVerifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validate user membership within Active Directory Domain without password authentication
        /// </summary>
        public async Task<IMembershipProcessingResult> ProcessMembershipAsync(PendingRequest request, IClientConfiguration clientConfig)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (clientConfig is null) throw new ArgumentNullException(nameof(clientConfig));

            var result = new MembershipProcessingResult();

            var userName = request.UserName;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning("Verification user' membership failed: can't find 'User-Name' attribute (messageId: {id}, from: {host:l}:{port})", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return result;
            }

            ILdapProfile profile = null;
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
                            profile = await _profileLoader.LoadAsync(clientConfig, connAdapter, user);
                        }

                        var res = _membershipVerifier.VerifyMembership(clientConfig, profile, domain, user);
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
    }
}