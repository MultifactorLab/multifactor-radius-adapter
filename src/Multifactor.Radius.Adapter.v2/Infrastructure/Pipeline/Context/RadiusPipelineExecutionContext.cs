using System.Net;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi.PrivacyMode;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Pipeline.Settings;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Core.RandomWaiterFeature;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

public class RadiusPipelineExecutionContext : IRadiusPipelineExecutionContext
{
    private readonly IPipelineExecutionSettings _settings;
    public ILdapProfile UserLdapProfile { get; set; }
    public IRadiusPacket RequestPacket { get; }
    public IRadiusPacket? ResponsePacket { get; set; }
    public IExecutionState ExecutionState { get; } = new ExecutionState();
    public IAuthenticationState AuthenticationState { get; set; } = new AuthenticationState();
    public IResponseInformation ResponseInformation { get; set; } = new ResponseInformation();
    public string MustChangePasswordDomain { get; set; }
    public IPEndPoint RemoteEndpoint { get; set; }
    public IPEndPoint? ProxyEndpoint { get; set; }
    public ILdapSchema? LdapSchema { get; set; }
    public UserPassphrase Passphrase { get; set; }
    public HashSet<string> UserGroups { get; set; } = new();
    public ILdapServerConfiguration LdapServerConfiguration => _settings.LdapServerConfiguration;
    public AuthenticatedClientCacheConfig AuthenticationCacheLifetime => _settings.AuthenticationCacheLifetime;
    public bool BypassSecondFactorWhenApiUnreachable => _settings.BypassSecondFactorWhenApiUnreachable;
    public AuthenticationSource FirstFactorAuthenticationSource => _settings.FirstFactorAuthenticationSource;
    public ApiCredential ApiCredential => _settings.ApiCredential;
    public IPEndPoint NpsServerEndpoint => _settings.NpsServerEndpoint;
    public PrivacyModeDescriptor PrivacyMode => _settings.PrivacyMode;
    public IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> RadiusReplyAttributes => _settings.RadiusReplyAttributes;
    public IPEndPoint ServiceClientEndpoint => _settings.ServiceClientEndpoint;
    public string SignUpGroups => _settings.SignUpGroups;
    public UserNameTransformRules UserNameTransformRules => _settings.UserNameTransformRules;
    public RandomWaiterConfig InvalidCredentialDelay => _settings.InvalidCredentialDelay;
    public PreAuthModeDescriptor PreAuthnMode => _settings.PreAuthnMode;
    public string ClientConfigurationName => _settings.ClientConfigurationName;
    public SharedSecret RadiusSharedSecret => _settings.RadiusSharedSecret;

    public RadiusPipelineExecutionContext(IPipelineExecutionSettings settings, IRadiusPacket requestPacket)
    {
        Throw.IfNull(settings, nameof(settings));
        Throw.IfNull(requestPacket, nameof(requestPacket));
        
        _settings = settings;
        RequestPacket = requestPacket;
    }

}