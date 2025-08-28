using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap.Forest;

public interface ILdapForestLoader
{
    public LdapImplementation LdapImplementation { get; }

    public IEnumerable<DistinguishedName> LoadTrustedDomains(ILdapConnection connection, ILdapSchema schema);

    public IEnumerable<string> LoadDomainSuffixes(ILdapConnection connection, ILdapSchema schema);
}