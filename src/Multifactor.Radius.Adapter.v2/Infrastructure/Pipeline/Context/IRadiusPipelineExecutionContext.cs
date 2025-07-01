using System.Net;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Auth;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi.PrivacyMode;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.Core.RandomWaiterFeature;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

public interface IRadiusPipelineExecutionContext
{
    ILdapProfile UserLdapProfile { get; set; }
    IRadiusPacket RequestPacket { get; }
    IRadiusPacket? ResponsePacket { get; set; }
    IAuthenticationState AuthenticationState { get; set; }
    IResponseInformation ResponseInformation { get; set; }
    IExecutionState ExecutionState { get; }
    string? MustChangePasswordDomain { get; set; }
    IPEndPoint RemoteEndpoint { get; set; }
    IPEndPoint? ProxyEndpoint { get; set; }
    ILdapSchema? LdapSchema { get; set; }
    UserPassphrase Passphrase { get; set; }
    HashSet<string> UserGroups { get; set; }
    ILdapServerConfiguration LdapServerConfiguration { get; }
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