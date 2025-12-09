using System.Net;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Domain;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Domain.Pipeline;
using Multifactor.Radius.Adapter.v2.Domain.Radius;
using Multifactor.Radius.Adapter.v2.Domain.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class RadiusPipelineExecutionContext
{
    private readonly PipelineExecutionSettings _settings;
    public ILdapProfile? UserLdapProfile { get; set; }
    public IRadiusPacket RequestPacket { get; }
    public IRadiusPacket? ResponsePacket { get; set; }
    public ExecutionState ExecutionState { get; } = new ExecutionState();
    public AuthenticationState AuthenticationState { get; set; } = new AuthenticationState();
    public ResponseInformation ResponseInformation { get; set; } = new ResponseInformation();
    public string MustChangePasswordDomain { get; set; }
    public IPEndPoint RemoteEndpoint => RequestPacket.RemoteEndpoint;
    public IPEndPoint? ProxyEndpoint => RequestPacket.ProxyEndpoint;
    public ILdapSchema? LdapSchema { get; set; }
    public UserPassphrase Passphrase { get; set; }
    public HashSet<string> UserGroups { get; set; } = [];
    public ILdapServerConfiguration? LdapServerConfiguration => _settings.LdapServerConfiguration;
    public AuthenticatedClientCacheConfig AuthenticationCacheLifetime => _settings.AuthenticationCacheLifetime;
    public bool BypassSecondFactorWhenApiUnreachable => _settings.BypassSecondFactorWhenApiUnreachable;
    public AuthenticationSource FirstFactorAuthenticationSource => _settings.FirstFactorAuthenticationSource;
    public ApiCredential ApiCredential => _settings.ApiCredential;
    public IReadOnlySet<IPEndPoint> NpsServerEndpoints  => _settings.NpsServerEndpoints;
    public TimeSpan NpsServerTimeout => _settings.NpsServerTimeout;
    public PrivacyModeDescriptor PrivacyMode => _settings.PrivacyMode;
    public IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> RadiusReplyAttributes => _settings.RadiusReplyAttributes;
    public IPEndPoint ServiceClientEndpoint => _settings.ServiceClientEndpoint;
    public string SignUpGroups => _settings.SignUpGroups;
    public UserNameTransformRules UserNameTransformRules => _settings.UserNameTransformRules;
    public RandomWaiterConfig InvalidCredentialDelay => _settings.InvalidCredentialDelay;
    public PreAuthModeDescriptor PreAuthnMode => _settings.PreAuthnMode;
    public string ClientConfigurationName => _settings.ClientConfigurationName;
    public SharedSecret RadiusSharedSecret => _settings.RadiusSharedSecret;
    public IReadOnlyCollection<IPAddressRange> IpWhiteList => _settings.LdapServerConfiguration?.IpWhiteList.Count > 0 ? _settings.LdapServerConfiguration.IpWhiteList : _settings.IpWhiteList; 
    public IReadOnlyList<string> ApiUrls => _settings.ApiUrls;
    public bool IsDomainAccount => RequestPacket.AccountType == AccountType.Domain;

    public RadiusPipelineExecutionContext(PipelineExecutionSettings settings, IRadiusPacket requestPacket)
    {
        Throw.IfNull(settings, nameof(settings));
        Throw.IfNull(requestPacket, nameof(requestPacket));
        
        _settings = settings;
        RequestPacket = requestPacket;
    }

}