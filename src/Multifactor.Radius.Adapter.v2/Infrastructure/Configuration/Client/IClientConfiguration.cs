using System.Net;
using Multifactor.Radius.Adapter.v2.Domain;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.MultifactorApi;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;

public interface IClientConfiguration
{
    IReadOnlyList<ILdapServerConfiguration> LdapServers { get; }
    AuthenticatedClientCacheConfig AuthenticationCacheLifetime { get; }
    bool BypassSecondFactorWhenApiUnreachable { get; }
    string CallingStationIdVendorAttribute { get; }
    AuthenticationSource FirstFactorAuthenticationSource { get; }
    ApiCredential ApiCredential { get; }
    string Name { get; }
    IReadOnlySet<IPEndPoint> NpsServerEndpoints { get; }
    TimeSpan NpsServerTimeout { get; }
    PrivacyModeDescriptor PrivacyMode { get; }
    IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> RadiusReplyAttributes { get; }
    string RadiusSharedSecret { get; }
    IPEndPoint ServiceClientEndpoint { get; }
    string SignUpGroups { get; }
    UserNameTransformRules UserNameTransformRules { get; }
    RandomWaiterConfig InvalidCredentialDelay { get; }
    PreAuthModeDescriptor PreAuthnMode { get; }
    IReadOnlyList<IPAddressRange> IpWhiteList { get; }
    IReadOnlyList<string> ApiUrls { get; }
}