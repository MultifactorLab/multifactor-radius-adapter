//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using System;
using System.Collections.ObjectModel;
using System.Net;

namespace MultiFactor.Radius.Adapter.Framework.Context
{
    /// <summary>
    /// Encapsulates all information about an individual RADIUS request.
    /// </summary>
    public class RadiusContext
    {
        public RadiusContext(
            IRadiusPacket request,
            IClientConfiguration clientConfiguration,
            IServiceProvider provider)
        {
            RequestPacket = request ?? throw new ArgumentNullException(nameof(request));
            Configuration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
            RequestServices = provider ?? throw new ArgumentNullException(nameof(provider));
            Authentication = new();
            Flags = new();
            Passphrase = UserPassphrase.Parse(request.TryGetUserPassword(), clientConfiguration.PreAuthnMode);
            Profile = LdapProfile.Empty;
        }

        public IPEndPoint RemoteEndpoint { get; set; }
        public IPEndPoint ProxyEndpoint { get; set; }

        /// <summary>
        /// Current request packet.
        /// </summary>
        public IRadiusPacket RequestPacket { get; }

        public RadiusPacketHeader Header => RequestPacket.Header;

        public IRadiusPacket ResponsePacket { get; set; }

        public PacketCode ResponseCode => Authentication.ToPacketCode();

        /// <summary>
        /// Context flags. Used for in-action configuring the pipeline behavior.
        /// </summary>
        public RadiusContextFlags Flags { get; }

        /// <summary>
        /// Challenge state.
        /// </summary>
        public string State { get; private set; }
        public string ReplyMessage { get; private set; }

        /// <summary>
        /// User-Name RADIUS attribute value.
        /// </summary>
        public string UserName => RequestPacket.UserName;

        public UserPassphrase Passphrase { get; private set; }

        /// <summary>
        /// Should use for 2FA request to MFA API.
        /// </summary>
        public string SecondFactorIdentity => Configuration.UseIdentityAttribute ? Profile.SecondFactorIdentity : UserName;

        /// <summary>
        /// memberof LDAP attribute value.
        /// </summary>
        public ReadOnlyCollection<string> UserGroups => Profile.MemberOf;

        public IServiceProvider RequestServices { get; }

        /// <summary>
        /// Client configuration.
        /// </summary>
        public IClientConfiguration Configuration { get; }

        public AuthenticationSource FirstFactorAuthenticationSource => Configuration.FirstFactorAuthenticationSource;
        public PreAuthMode PreAuthMode => Configuration.PreAuthnMode.Mode;

        /// <inheritdoc cref="AuthenticationState"/>
        public AuthenticationState Authentication { get; private set; }

        /// <inheritdoc cref="AuthenticationState.SetFirstFactor(AuthenticationCode)"/>
        public void SetFirstFactorAuth(AuthenticationCode code) => Authentication.SetFirstFactor(code);

        /// <inheritdoc cref="AuthenticationState.SetSecondFactor(AuthenticationCode)"/>
        public void SetSecondFactorAuth(AuthenticationCode code) => Authentication.SetSecondFactor(code);

        /// <summary>
        /// Current user LDAP profile.
        /// </summary>
        public LdapProfile Profile { get; private set; }

        /// <summary>
        /// Replaces the current user's LDAP profile.
        /// </summary>
        /// <param name="profile">New profile</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void UpdateProfile(LdapProfile profile)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        /// <summary>
        /// Replaces the RADIUS attribute value.
        /// </summary>
        /// <param name="attribute">RADIUS attribute.</param>
        /// <param name="newValue">New value.</param>
        public void TransformRadiusRequestAttribute(string attribute, string newValue) => RequestPacket.AddTransformation(attribute, newValue);

        public void SetMessageState(string state)
        {
            State = state;
        }

        public void SetReplyMessage(string msg)
        {
            ReplyMessage = msg;
        }

        /// <summary>
        /// Replaces some context data from the other context.
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Update(RadiusContext context)
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
