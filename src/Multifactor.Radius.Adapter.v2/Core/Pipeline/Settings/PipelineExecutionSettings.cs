using System.Net;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi.PrivacyMode;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.RandomWaiterFeature;

namespace Multifactor.Radius.Adapter.v2.Core.Pipeline.Settings;

public class PipelineExecutionSettings : IPipelineExecutionSettings
{
    private readonly IPipelineCommonSettings _configuration;
    public ILdapServerConfiguration LdapServer { get; }
    public AuthenticatedClientCacheConfig AuthenticationCacheLifetime => _configuration.AuthenticationCacheLifetime;
    public bool BypassSecondFactorWhenApiUnreachable => _configuration.BypassSecondFactorWhenApiUnreachable;
    public AuthenticationSource FirstFactorAuthenticationSource => _configuration.FirstFactorAuthenticationSource;
    public ApiCredential ApiCredential => _configuration.ApiCredential;
    public IPEndPoint NpsServerEndpoint => _configuration.NpsServerEndpoint;
    public PrivacyModeDescriptor PrivacyMode => _configuration.PrivacyMode;
    public IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> RadiusReplyAttributes => _configuration.RadiusReplyAttributes;
    public IPEndPoint ServiceClientEndpoint => _configuration.ServiceClientEndpoint;
    public string SignUpGroups => _configuration.SignUpGroups;
    public UserNameTransformRules UserNameTransformRules => _configuration.UserNameTransformRules;
    public RandomWaiterConfig InvalidCredentialDelay => _configuration.InvalidCredentialDelay;
    public PreAuthModeDescriptor PreAuthnMode => _configuration.PreAuthnMode;
    public SharedSecret RadiusSharedSecret => _configuration.RadiusSharedSecret;
    public string ClientConfigurationName => _configuration.ClientConfigurationName;
    
    public PipelineExecutionSettings(IPipelineCommonSettings commonConfiguration, ILdapServerConfiguration ldapServer)
    {
        Throw.IfNull(commonConfiguration, nameof(commonConfiguration));
        Throw.IfNull(ldapServer, nameof(ldapServer));
        
        _configuration = commonConfiguration;
        LdapServer = ldapServer;
    }
}