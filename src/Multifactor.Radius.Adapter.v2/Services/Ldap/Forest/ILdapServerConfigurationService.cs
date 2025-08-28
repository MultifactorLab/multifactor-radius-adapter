using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap.Forest;

public interface ILdapServerConfigurationService
{
    IEnumerable<ILdapServerConfiguration> DuplicateConfigurationForDn(IEnumerable<DistinguishedName> targetDomains, ILdapServerConfiguration initialConfiguration);
}