﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Services.Ldap;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.Processing
{
    /// <summary>
    /// Authenticate request at LDAP/Active Directory Domain with user-name and password
    /// </summary>
    public class LdapFirstFactorAuthenticationProcessor : IFirstFactorAuthenticationProcessor
    {
        private readonly LdapService _ldapService;
        private readonly ILogger<LdapFirstFactorAuthenticationProcessor> _logger;

        public LdapFirstFactorAuthenticationProcessor(
            LdapService ldapService,
            ILogger<LdapFirstFactorAuthenticationProcessor> logger)
        {
            _ldapService = ldapService ?? throw new ArgumentNullException(nameof(ldapService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public AuthenticationSource AuthenticationSource => AuthenticationSource.ActiveDirectory | AuthenticationSource.Ldap;

        public async Task<PacketCode> ProcessFirstAuthFactorAsync(RadiusContext context)
        {
            var userName = UserNameTransformation.Transform(context.UserName, context.Configuration.UserNameTransformRules.BeforeFirstFactor);

            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("Can't find User-Name in message id={id} from {host:l}:{port}", context.RequestPacket.Header.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            var password = context.Passphrase.Password;

            if (string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Can't find User-Password in message id={id} from {host:l}:{port}", context.RequestPacket.Header.Identifier, context.RemoteEndpoint.Address, context.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            //check all hosts
            foreach (var ldapUri in context.Configuration.SplittedActiveDirectoryDomains)
            {
                try
                {
                    await _ldapService.VerifyCredential(userName, password, ldapUri, context);
                    var isValid = await _ldapService.VerifyMembership(userName, password, ldapUri, context);
                    if (isValid)
                    {
                        return PacketCode.AccessAccept;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Verification user '{user:l}' at {ldapUri:l} failed: {message:l}", userName, ldapUri, ex.Message);
                }
            }

            return PacketCode.AccessReject;
        }
    }
}
