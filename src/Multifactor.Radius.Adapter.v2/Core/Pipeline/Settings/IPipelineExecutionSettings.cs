using System.Net;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi.PrivacyMode;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.RandomWaiterFeature;

namespace Multifactor.Radius.Adapter.v2.Core.Pipeline.Settings;

public interface IPipelineExecutionSettings
{
    IReadOnlyCollection<ILdapServerConfiguration>  LdapServers { get; }
    public ILdapServerConfiguration LdapServerConfiguration { get; }
    AuthenticatedClientCacheConfig AuthenticationCacheLifetime { get; }
    bool BypassSecondFactorWhenApiUnreachable { get; }
    AuthenticationSource FirstFactorAuthenticationSource { get; }
    ApiCredential ApiCredential { get; }
    IPEndPoint NpsServerEndpoint { get; }
    PrivacyModeDescriptor PrivacyMode { get; }
    IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> RadiusReplyAttributes { get; }
    IPEndPoint ServiceClientEndpoint { get; }
    string SignUpGroups { get; }
    UserNameTransformRules UserNameTransformRules { get; }
    RandomWaiterConfig InvalidCredentialDelay { get; }
    PreAuthModeDescriptor PreAuthnMode { get; }
    string ClientConfigurationName { get; }
    SharedSecret RadiusSharedSecret { get; }
}