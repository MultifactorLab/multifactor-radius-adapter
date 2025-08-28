using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap.Forest;

public class LdapServerConfigurationService : ILdapServerConfigurationService
{
    public IEnumerable<ILdapServerConfiguration> DuplicateConfigurationForDn(IEnumerable<DistinguishedName> targetDomains, ILdapServerConfiguration initialConfiguration)
    {
        return targetDomains.Select(x => CreateConfigurationWithDn(x, initialConfiguration));
    }
    
    private ILdapServerConfiguration CreateConfigurationWithDn(DistinguishedName trustedDomain, ILdapServerConfiguration initialConfiguration)
    {
        var config = new LdapServerConfiguration(LdapNamesUtils.DnToFqdn(trustedDomain), initialConfiguration.UserName, initialConfiguration.Password);
        var settings = new LdapServerInitializeRequest(initialConfiguration);
        config.Initialize(settings);
        return config;
    }
}