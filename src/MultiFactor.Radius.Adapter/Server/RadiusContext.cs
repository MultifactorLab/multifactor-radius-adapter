//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Core.Radius;
using System;
using System.Collections.Generic;
using System.Net;

namespace MultiFactor.Radius.Adapter.Server
{
    public class RadiusContext
    {
        private ILdapProfile _ldapProfile;

        public RadiusContext(IClientConfiguration clientConfiguration, IRadiusResponseSender radiusResponseSender, IServiceProvider provider)
        {
            ReceivedAt = DateTime.Now;
            ResponseCode = PacketCode.AccessReject;
            UserGroups = new List<string>();
            ClientConfiguration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
            ResponseSender = radiusResponseSender ?? throw new ArgumentNullException(nameof(radiusResponseSender));
            RequestServices = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public IPEndPoint RemoteEndpoint { get; init; }
        public IPEndPoint ProxyEndpoint { get; init; }

        public IRadiusPacket RequestPacket { get; init; }
        public IRadiusPacket ResponsePacket { get; set; }

        public DateTime ReceivedAt { get; set; }
        public PacketCode ResponseCode { get; set; }
        public string State { get; set; }
        public string ReplyMessage { get; set; }
        public string UserName { get; set; }
        public string Upn => _ldapProfile?.Upn;
        public string DisplayName => _ldapProfile?.DisplayName;
        public string UserPhone => _ldapProfile?.Phone;
        public string EmailAddress => _ldapProfile?.Email;
        public bool Bypass2Fa { get; set; }

        /// <summary>
        /// Should use for 2FA request to MFA API.
        /// </summary>
        public string SecondFactorIdentity => ClientConfiguration.UseIdentityAttribyte ? _ldapProfile?.SecondFactorIdentity : UserName;
        public IList<string> UserGroups { get; set; }
        public IDictionary<string, object> LdapAttrs { get; set; }
        public IServiceProvider RequestServices { get; set; }
        public IClientConfiguration ClientConfiguration { get; }
        public IRadiusResponseSender ResponseSender { get; }

        public void SetProfile(ILdapProfile profile)
        {
            _ldapProfile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public void CopyProfileToContext(RadiusContext other)
        {
            other.SetProfile(_ldapProfile);
        }
    }
}
