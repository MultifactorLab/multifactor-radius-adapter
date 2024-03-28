//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Elastic.CommonSchema;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace MultiFactor.Radius.Adapter.Framework.Context
{
    /// <summary>
    /// Encapsulates all information about an individual RADIUS request.
    /// </summary>
    public class RadiusContext
    {
        private ILdapProfile _ldapProfile;

        public RadiusContext(IRadiusPacket request, IClientConfiguration clientConfiguration, IUdpClient udpClient, IServiceProvider provider)
        {
            RequestPacket = request ?? throw new ArgumentNullException(nameof(request));
            ReceivedAt = DateTime.Now;
            ResponseCode = PacketCode.AccessReject;
            UserGroups = new List<string>();
            Configuration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
            UdpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
            RequestServices = provider ?? throw new ArgumentNullException(nameof(provider));
            Authentication = new();
            Flags = new();
            Passphrase = UserPassphrase.Parse(request.TryGetUserPassword(), clientConfiguration.PreAuthnMode);
        }

        public IPEndPoint RemoteEndpoint { get; set; }
        public IPEndPoint ProxyEndpoint { get; init; }

        public IRadiusPacket RequestPacket { get; }
        public RadiusPacketHeader Header => RequestPacket.Header;
        public IRadiusPacket ResponsePacket { get; set; }

        public RadiusContextFlags Flags { get; }

        public DateTime ReceivedAt { get; set; }
        public PacketCode ResponseCode { get; set; }
        public string State { get; set; }
        public string ReplyMessage { get; set; }

        public void SetChallengeState(string state, string replyMessage)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                throw new ArgumentException($"'{nameof(state)}' cannot be null or whitespace.", nameof(state));
            }

            State = state;
            ReplyMessage = replyMessage;
        }

        public string UserName { get; set; }
        public UserPassphrase Passphrase { get; }

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
        public IUdpClient UdpClient { get; }
        public IClientConfiguration Configuration { get; }

        public AuthenticationSource FirstFactorAuthenticationSource => Configuration.FirstFactorAuthenticationSource;
        public PreAuthMode PreAuthMode => Configuration.PreAuthnMode.Mode;


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
