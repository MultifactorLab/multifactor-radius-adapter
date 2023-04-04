using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Configuration.Features.AuthenticatedClientCacheFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature;
using MultiFactor.Radius.Adapter.Server;
using System.Collections.Generic;
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration
{
    public interface IClientConfigurationBuilder
    {
        IClientConfigurationBuilder SetBypassSecondFactorWhenApiUnreachable(bool val);
        IClientConfigurationBuilder SetPrivacyMode(PrivacyModeDescriptor val);
        IClientConfigurationBuilder SetActiveDirectoryDomain(string val);
        IClientConfigurationBuilder SetLdapBindDn(string val);
        IClientConfigurationBuilder SetActiveDirectoryGroup(string[] val);
        IClientConfigurationBuilder SetActiveDirectory2FaGroup(string[] val);
        IClientConfigurationBuilder SetActiveDirectory2FaBypassGroup(string[] val);
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
