using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;

public interface IForestFilter
{
    IEnumerable<LdapForestEntry> FilterDomains(IEnumerable<LdapForestEntry> domains, IPermissionRules permission);

    IEnumerable<LdapForestEntry> FilterSuffixes(IEnumerable<LdapForestEntry> domains, IPermissionRules permission);
}