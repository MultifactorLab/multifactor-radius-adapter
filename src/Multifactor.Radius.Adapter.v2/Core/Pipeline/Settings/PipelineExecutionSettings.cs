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
    private readonly IClientConfiguration _configuration;
    private readonly SharedSecret _sharedSecret;
    public IReadOnlyCollection<ILdapServerConfiguration> LdapServers => _configuration.LdapServers;
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
    public SharedSecret RadiusSharedSecret => _sharedSecret;
    public string ClientConfigurationName => _configuration.Name;
    
    public PipelineExecutionSettings(IClientConfiguration clientConfiguration)
    {
        Throw.IfNull(clientConfiguration, nameof(clientConfiguration));
        
        _configuration = clientConfiguration;
        _sharedSecret = new SharedSecret(clientConfiguration.RadiusSharedSecret);
    }
}