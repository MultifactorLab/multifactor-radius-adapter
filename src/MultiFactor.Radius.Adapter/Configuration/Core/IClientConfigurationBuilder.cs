using MultiFactor.Radius.Adapter.Configuration.Features.AuthenticatedClientCacheFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;
using MultiFactor.Radius.Adapter.Server;
using System.Collections.Generic;
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration.Core
{
    public interface IClientConfigurationBuilder
    {
        IClientConfigurationBuilder SetBypassSecondFactorWhenApiUnreachable(bool val);
        IClientConfigurationBuilder SetPrivacyMode(PrivacyModeDescriptor val);
        IClientConfigurationBuilder SetActiveDirectoryDomain(string val);
        IClientConfigurationBuilder SetLdapBindDn(string val);

        IClientConfigurationBuilder AddActiveDirectoryGroup(string val);
        IClientConfigurationBuilder AddActiveDirectoryGroups(IEnumerable<string> values);

        IClientConfigurationBuilder AddActiveDirectory2FaGroup(string val);
        IClientConfigurationBuilder AddActiveDirectory2FaGroups(IEnumerable<string> values);

        IClientConfigurationBuilder AddActiveDirectory2FaBypassGroup(string val);
        IClientConfigurationBuilder AddActiveDirectory2FaBypassGroups(IEnumerable<string> values);

        IClientConfigurationBuilder AddPhoneAttribute(string phoneAttr);
        IClientConfigurationBuilder AddPhoneAttributes(IEnumerable<string> attributes);

        IClientConfigurationBuilder SetLoadActiveDirectoryNestedGroups(bool val);
        IClientConfigurationBuilder SetUseUpnAsIdentity(bool val);
        IClientConfigurationBuilder SetServiceClientEndpoint(IPEndPoint val);
        IClientConfigurationBuilder SetNpsServerEndpoint(IPEndPoint val);
        IClientConfigurationBuilder SetServiceAccountUser(string val);
        IClientConfigurationBuilder SetServiceAccountPassword(string val);
        IClientConfigurationBuilder SetSignUpGroups(string val);
        IClientConfigurationBuilder SetAuthenticationCacheLifetime(AuthenticatedClientCacheConfig val);

        IClientConfigurationBuilder SetRadiusReplyAttributes(IDictionary<string, List<RadiusReplyAttributeValue>> val);

        IClientConfigurationBuilder AddUserNameTransformRule(UserNameTransformRulesElement rule);
        IClientConfigurationBuilder SetCallingStationIdVendorAttribute(string val);

        IClientConfiguration Build();
    }
}
