using System.Net;
using Multifactor.Radius.Adapter.v2.Domain;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.MultifactorApi.Dto;

public class CreateSecondFactorRequest
{
    public ILdapProfile? UserProfile { get; }
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
    public IReadOnlyList<string> ApiUrls { get; }
    public bool ApiResponseCacheEnabled { get; }

    public CreateSecondFactorRequest(RadiusPipelineExecutionContext context, bool cacheEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.RequestPacket);
        ArgumentNullException.ThrowIfNull(context.RemoteEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ClientConfigurationName);
        ArgumentNullException.ThrowIfNull(context.AuthenticationCacheLifetime);
        ArgumentNullException.ThrowIfNull(context.PrivacyMode);
        ArgumentNullException.ThrowIfNull(context.Passphrase);
        ArgumentNullException.ThrowIfNull(context.PreAuthnMode);
        ArgumentNullException.ThrowIfNull(context.UserNameTransformRules);
        ArgumentNullException.ThrowIfNull(context.ApiCredential);

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
        IdentityAttribute = context.LdapServerConfiguration?.IdentityAttribute;
        BypassSecondFactorWhenApiUnreachable = context.BypassSecondFactorWhenApiUnreachable;
        PhoneAttributesNames = context.LdapServerConfiguration?.PhoneAttributes ?? new List<string>();
        ApiResponseCacheEnabled = cacheEnabled;
        ApiUrls = context.ApiUrls;
    }
}