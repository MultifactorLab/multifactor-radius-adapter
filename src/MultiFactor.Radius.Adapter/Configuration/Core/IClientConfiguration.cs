using MultiFactor.Radius.Adapter.Configuration.Features.AuthenticatedClientCacheFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;
using MultiFactor.Radius.Adapter.Server;
using System.Collections.Generic;
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration.Core
{
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
        string MultifactorApiKey { get; }
        string MultiFactorApiSecret { get; }
        string Name { get; }
        IPEndPoint NpsServerEndpoint { get; }
        string[] PhoneAttributes { get; }
        PrivacyModeDescriptor PrivacyMode { get; }
        IDictionary<string, List<RadiusReplyAttributeValue>> RadiusReplyAttributes { get; }
        string RadiusSharedSecret { get; }
        string ServiceAccountPassword { get; }
        string ServiceAccountUser { get; }
        IPEndPoint ServiceClientEndpoint { get; }
        string SignUpGroups { get; }
        string[] SplittedActiveDirectoryDomains { get; }
        UserNameTransformRulesElement[] UserNameTransformRules { get; }
        bool UseUpnAsIdentity { get; }
        IList<string> GetLdapReplyAttributes();
        bool ShouldLoadUserGroups();
    }
}