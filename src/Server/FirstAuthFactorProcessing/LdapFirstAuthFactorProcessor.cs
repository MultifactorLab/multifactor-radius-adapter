//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Services.Ldap;
using Serilog;
using System;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Server.FirstAuthFactorProcessing
{
    /// <summary>
    /// Authenticate request at LDAP/Active Directory Domain with user-name and password
    /// </summary>
    public class LdapFirstAuthFactorProcessor : IFirstAuthFactorProcessor
    {
        private readonly IServiceConfiguration _serviceConfiguration;
        private readonly LdapService _ldapService;
        private readonly ILogger _logger;

        public LdapFirstAuthFactorProcessor(IServiceConfiguration serviceConfiguration,
            LdapService ldapService,
            ILogger logger)
        {
            _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
            _ldapService = ldapService ?? throw new ArgumentNullException(nameof(ldapService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public AuthenticationSource AuthenticationSource => AuthenticationSource.ActiveDirectory | AuthenticationSource.Ldap;

        public async Task<PacketCode> ProcessFirstAuthFactorAsync(PendingRequest request, IClientConfiguration clientConfig)
        {
            var userName = request.UserName;

            if (string.IsNullOrEmpty(userName))
            {
                _logger.Warning("Can't find User-Name in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            var password = request.RequestPacket.TryGetUserPassword();

            if (string.IsNullOrEmpty(password))
            {
                _logger.Warning("Can't find User-Password in message id={id} from {host:l}:{port}", request.RequestPacket.Identifier, request.RemoteEndpoint.Address, request.RemoteEndpoint.Port);
                return PacketCode.AccessReject;
            }

            //check all hosts
            var ldapUriList = clientConfig.ActiveDirectoryDomain.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var ldapUri in ldapUriList)
            {
                var isValid = await _ldapService.VerifyCredential(userName, password, ldapUri, request, clientConfig);
                if (isValid)
                {
                    return PacketCode.AccessAccept;
                }
            }

            return PacketCode.AccessReject;
        }
    }
}
