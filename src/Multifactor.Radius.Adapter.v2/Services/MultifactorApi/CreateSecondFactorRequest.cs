using System.Net;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi.PrivacyMode;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

public class CreateSecondFactorRequest
{
    public ILdapProfile UserProfile { get; }
    public IRadiusPacket RequestPacket { get; }
    public IPEndPoint RemoteEndpoint { get; }
    public string ConfigName { get; }
    public PrivacyModeDescriptor PrivacyMode { get; }
    public AuthenticatedClientCacheConfig AuthenticationCacheLifetime { get; }
    public string? SignUpGroups { get; }
    public UserPassphrase Passphrase { get; }
    public PreAuthModeDescriptor PreAuthnMode { get; }
    public AuthenticationSource FirstFactorAuthenticationSource { get; }
    public UserNameTransformRules UserNameTransformRules { get; }
    public ApiCredential ApiCredential { get; }
    public string? IdentityAttribute { get; }
    public bool BypassSecondFactorWhenApiUnreachable { get; }
    public IReadOnlyList<string> PhoneAttributesNames { get; }

    public CreateSecondFactorRequest(IRadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.UserLdapProfile);
        ArgumentNullException.ThrowIfNull(context.RequestPacket);
        ArgumentNullException.ThrowIfNull(context.RemoteEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ClientConfigurationName);
        ArgumentNullException.ThrowIfNull(context.AuthenticationCacheLifetime);
        ArgumentNullException.ThrowIfNull(context.PrivacyMode);
        ArgumentNullException.ThrowIfNull(context.Passphrase);
        ArgumentNullException.ThrowIfNull(context.PreAuthnMode);
        ArgumentNullException.ThrowIfNull(context.UserNameTransformRules);
        ArgumentNullException.ThrowIfNull(context.ApiCredential);
        ArgumentNullException.ThrowIfNull(context.LdapServerConfiguration);
        
        UserProfile = context.UserLdapProfile;
        RequestPacket = context.RequestPacket;
        RemoteEndpoint = context.RemoteEndpoint;
        ConfigName = context.ClientConfigurationName;
        AuthenticationCacheLifetime = context.AuthenticationCacheLifetime;
        PrivacyMode = context.PrivacyMode;
        SignUpGroups = context.SignUpGroups;
        Passphrase = context.Passphrase;
        PreAuthnMode = context.PreAuthnMode;
        FirstFactorAuthenticationSource = context.FirstFactorAuthenticationSource;
        UserNameTransformRules = context.UserNameTransformRules;
        ApiCredential = context.ApiCredential;
        IdentityAttribute = context.LdapServerConfiguration.IdentityAttribute;
        BypassSecondFactorWhenApiUnreachable = context.BypassSecondFactorWhenApiUnreachable;
        PhoneAttributesNames = context.LdapServerConfiguration.PhoneAttributes;
    }
}