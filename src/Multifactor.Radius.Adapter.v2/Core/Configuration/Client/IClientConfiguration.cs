using System.Net;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi.PrivacyMode;
using Multifactor.Radius.Adapter.v2.Core.RandomWaiterFeature;

namespace Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

public interface IClientConfiguration
{
    IReadOnlyList<LdapServerConfiguration> LdapServers { get; }
    AuthenticatedClientCacheConfig AuthenticationCacheLifetime { get; }
    bool BypassSecondFactorWhenApiUnreachable { get; }
    string CallingStationIdVendorAttribute { get; }
    AuthenticationSource FirstFactorAuthenticationSource { get; }
    ApiCredential ApiCredential { get; }
    string Name { get; }
    IPEndPoint NpsServerEndpoint { get; }
    PrivacyModeDescriptor PrivacyMode { get; }
    IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> RadiusReplyAttributes { get; }
    string RadiusSharedSecret { get; }
    IPEndPoint ServiceClientEndpoint { get; }
    string SignUpGroups { get; }
    UserNameTransformRules UserNameTransformRules { get; }
    RandomWaiterConfig InvalidCredentialDelay { get; }
    PreAuthModeDescriptor PreAuthnMode { get; }
}