using System.Collections.Generic;
using System.Net;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.AuthenticatedClientCacheFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.RandomWaiterFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.UserNameTransform;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;

public interface IClientConfiguration
{
    string[] ActiveDirectory2FaBypassGroup { get; }
    string[] ActiveDirectory2FaGroup { get; }
    string ActiveDirectoryDomain { get; }
    string[] ActiveDirectoryGroups { get; }
    AuthenticatedClientCacheConfig AuthenticationCacheLifetime { get; }
    bool BypassSecondFactorWhenApiUnreachable { get; }
    string CallingStationIdVendorAttribute { get; }
    bool CheckMembership { get; }
    AuthenticationSource FirstFactorAuthenticationSource { get; }
    string LdapBindDn { get; }
    bool LoadActiveDirectoryNestedGroups { get; }
    ApiCredential ApiCredential { get; }
    string Name { get; }
    IPEndPoint NpsServerEndpoint { get; }
    string[] PhoneAttributes { get; }
    PrivacyModeDescriptor PrivacyMode { get; }
    IReadOnlyDictionary<string, RadiusReplyAttributeValue[]> RadiusReplyAttributes { get; }
    string RadiusSharedSecret { get; }
    string ServiceAccountPassword { get; }
    string ServiceAccountUser { get; }
    IPEndPoint ServiceClientEndpoint { get; }
    string SignUpGroups { get; }
    string[] SplittedActiveDirectoryDomains { get; }
    UserNameTransformRules UserNameTransformRules { get; }
    public string TwoFAIdentityAttribute { get; }
    public bool UseIdentityAttribute => !string.IsNullOrEmpty(TwoFAIdentityAttribute);
    bool ShouldLoadUserGroups();
    RandomWaiterConfig InvalidCredentialDelay { get; }
    PreAuthModeDescriptor PreAuthnMode { get; }
    bool IsFreeIpa { get; }
}