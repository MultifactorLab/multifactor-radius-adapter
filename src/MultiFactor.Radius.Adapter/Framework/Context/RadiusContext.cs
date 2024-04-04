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
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;

namespace MultiFactor.Radius.Adapter.Framework.Context
{
    /// <summary>
    /// Encapsulates all information about an individual RADIUS request.
    /// </summary>
    public class RadiusContext
    {
        public RadiusContext(IRadiusPacket request,
            IClientConfiguration clientConfiguration,
            IUdpClient udpClient,
            IServiceProvider provider)
        {
            RequestPacket = request ?? throw new ArgumentNullException(nameof(request));
            ReceivedAt = DateTime.Now;
            ResponseCode = PacketCode.AccessReject;
            Configuration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
            UdpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
            RequestServices = provider ?? throw new ArgumentNullException(nameof(provider));
            Authentication = new();
            Flags = new();
            Passphrase = UserPassphrase.Parse(request.TryGetUserPassword(), clientConfiguration.PreAuthnMode);
            Profile = LdapProfile.Empty;
        }

        public IPEndPoint RemoteEndpoint { get; set; }
        public IPEndPoint ProxyEndpoint { get; set; }

        public IRadiusPacket RequestPacket { get; }
        public RadiusPacketHeader Header => RequestPacket.Header;
        public IRadiusPacket ResponsePacket { get; set; }

        public RadiusContextFlags Flags { get; }

        public DateTime ReceivedAt { get; }
        public PacketCode ResponseCode { get; set; }

        /// <summary>
        /// Challenge state.
        /// </summary>
        public string State { get; private set; }

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

        public string UserName => RequestPacket.UserName;

        public UserPassphrase Passphrase { get; private set; }

        /// <summary>
        /// Should use for 2FA request to MFA API.
        /// </summary>
        public string SecondFactorIdentity => Configuration.UseIdentityAttribute ? Profile.SecondFactorIdentity : UserName;
        public ReadOnlyCollection<string> UserGroups => Profile.MemberOf;

        public IServiceProvider RequestServices { get; }
        public IUdpClient UdpClient { get; }
        public IClientConfiguration Configuration { get; }

        public AuthenticationSource FirstFactorAuthenticationSource => Configuration.FirstFactorAuthenticationSource;
        public PreAuthMode PreAuthMode => Configuration.PreAuthnMode.Mode;


        public AuthenticationState Authentication { get; private set; }
        public void SetFirstFactorAuth(AuthenticationCode code) => Authentication.SetFirstFactor(code);
        public void SetSecondFactorAuth(AuthenticationCode code) => Authentication.SetSecondFactor(code);

        public LdapProfile Profile { get; private set; }

        public void UpdateProfile(LdapProfile profile)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public void CopyProfileToContext(RadiusContext other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // null if no AD request. winlogon, for example
            other.UpdateProfile(Profile);
        }

        public void TransformRadiusRequestAttribute(string attribute, string newValue) => RequestPacket.AddTransformation(attribute, newValue);

        public void SetMessageState(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                throw new ArgumentException($"'{nameof(state)}' cannot be null or whitespace.", nameof(state));
            }

            State = state;
        }

        public void UpdateFromChallengeRequest(RadiusContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ResponsePacket = context.ResponsePacket;
            Profile.UpdateAttributes(context.Profile.Attributes);
            Authentication = context.Authentication;
            Passphrase = context.Passphrase;
        }
    }
}
