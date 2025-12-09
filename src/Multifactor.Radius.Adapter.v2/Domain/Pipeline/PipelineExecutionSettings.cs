using System.Net;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Domain.Radius;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Domain.Pipeline;

public class PipelineExecutionSettings
{
    private readonly IClientConfiguration _configuration;
    private readonly SharedSecret _sharedSecret;
    public ILdapServerConfiguration? LdapServerConfiguration { get; }
    public AuthenticatedClientCacheConfig AuthenticationCacheLifetime => _configuration.AuthenticationCacheLifetime;
    public bool BypassSecondFactorWhenApiUnreachable => _configuration.BypassSecondFactorWhenApiUnreachable;
    public AuthenticationSource FirstFactorAuthenticationSource => _configuration.FirstFactorAuthenticationSource;
    public ApiCredential ApiCredential => _configuration.ApiCredential;
    public IReadOnlySet<IPEndPoint> NpsServerEndpoints => _configuration.NpsServerEndpoints;
    public TimeSpan NpsServerTimeout => _configuration.NpsServerTimeout;
    public PrivacyModeDescriptor PrivacyMode => _configuration.PrivacyMode;
    public IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> RadiusReplyAttributes => _configuration.RadiusReplyAttributes;
    public IPEndPoint ServiceClientEndpoint => _configuration.ServiceClientEndpoint;
    public string SignUpGroups => _configuration.SignUpGroups;
    public UserNameTransformRules UserNameTransformRules => _configuration.UserNameTransformRules;
    public RandomWaiterConfig InvalidCredentialDelay => _configuration.InvalidCredentialDelay;
    public PreAuthModeDescriptor PreAuthnMode => _configuration.PreAuthnMode;
    public SharedSecret RadiusSharedSecret => _sharedSecret;
    public IReadOnlyList<string> ApiUrls => _configuration.ApiUrls;
    public IReadOnlyList<IPAddressRange> IpWhiteList => _configuration.IpWhiteList;
    public string ClientConfigurationName => _configuration.Name;
    
    public PipelineExecutionSettings(IClientConfiguration clientConfiguration, ILdapServerConfiguration? ldapServerConfiguration = null)
    {
        Throw.IfNull(clientConfiguration, nameof(clientConfiguration));
        
        _configuration = clientConfiguration;
        _sharedSecret = new SharedSecret(clientConfiguration.RadiusSharedSecret);
        LdapServerConfiguration = ldapServerConfiguration;
    }
}