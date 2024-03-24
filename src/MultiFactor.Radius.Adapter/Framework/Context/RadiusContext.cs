//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server;
using System;
using System.Collections.Generic;
using System.Net;

namespace MultiFactor.Radius.Adapter.Framework.Context
{
    /// <summary>
    /// Encapsulates all information about an individual RADIUS request.
    /// </summary>
    public class RadiusContext
    {
        private ILdapProfile _ldapProfile;

        public RadiusContext(IClientConfiguration clientConfiguration, IUdpClient udpClient, IServiceProvider provider)
        {
            ReceivedAt = DateTime.Now;
            ResponseCode = PacketCode.AccessReject;
            UserGroups = new List<string>();
            Configuration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
            UdpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
            RequestServices = provider ?? throw new ArgumentNullException(nameof(provider));
            Authentication = new();
            Flags = new();
        }

        public IPEndPoint RemoteEndpoint { get; set; }
        public IPEndPoint ProxyEndpoint { get; init; }

        public IRadiusPacket RequestPacket { get; init; }
        public RadiusPacketHeader Header => RequestPacket.Header;
        public IRadiusPacket ResponsePacket { get; set; }

        public RadiusContextFlags Flags { get; }

        public DateTime ReceivedAt { get; set; }
        public PacketCode ResponseCode { get; set; }
        public string State { get; set; }
        public string ReplyMessage { get; set; }

        public string UserName { get; set; }
        public UserPassphrase Passphrase { get; init; }

        public string Upn => _ldapProfile?.Upn;
        public string DisplayName => _ldapProfile?.DisplayName;
        public string UserPhone => _ldapProfile?.Phone;
        public string EmailAddress => _ldapProfile?.Email;

        /// <summary>
        /// Should use for 2FA request to MFA API.
        /// </summary>
        public string SecondFactorIdentity => Configuration.UseIdentityAttribute ? _ldapProfile?.SecondFactorIdentity : UserName;
        public IList<string> UserGroups { get; set; }
        public IDictionary<string, object> LdapAttrs { get; set; }
        public IServiceProvider RequestServices { get; set; }

        public IClientConfiguration Configuration { get; }
        public AuthenticationSource FirstFactorAuthenticationSource => Configuration.FirstFactorAuthenticationSource;
        public PreAuthMode PreAuthMode => Configuration.PreAuthnMode.Mode;

        public IUdpClient UdpClient { get; }

        public AuthenticationState Authentication { get; }
        public void SetFirstFactorAuth(AuthenticationCode code) => Authentication.SetFirstFactor(code);
        public void SetSecondFactorAuth(AuthenticationCode code) => Authentication.SetSecondFactor(code);

        public void SetProfile(ILdapProfile profile)
        {
            _ldapProfile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public void CopyProfileToContext(RadiusContext other)
        {
            // null if no AD request. winlogon, for example
            if (_ldapProfile != null)
            {
                other.SetProfile(_ldapProfile);
            }
        }
    }
}
