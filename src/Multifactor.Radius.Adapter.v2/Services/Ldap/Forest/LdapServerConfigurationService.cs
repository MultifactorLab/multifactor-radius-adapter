using Multifactor.Core.Ldap;
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
        var connectionString = new LdapConnectionString(initialConfiguration.ConnectionString);
        var trustedLdapDomain = LdapNamesUtils.DnToFqdn(trustedDomain);
        var trustedConnectionString = connectionString.CopySchemaAndPort(trustedLdapDomain);
        var config = new LdapServerConfiguration(trustedConnectionString.WellFormedLdapUrl, initialConfiguration.UserName, initialConfiguration.Password);
        var settings = new LdapServerInitializeRequest(initialConfiguration);
        config.Initialize(settings);
        return config;
    }
}