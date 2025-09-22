using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;

public class ForestFilter : IForestFilter
{
    public IEnumerable<LdapForestEntry> FilterDomains(IEnumerable<LdapForestEntry> domains, IPermissionRules permission)
    {
        ArgumentNullException.ThrowIfNull(domains);
        ArgumentNullException.ThrowIfNull(permission);
        
        return domains.Where(x => permission.IsPermitted(LdapNamesUtils.DnToFqdn(x.Schema.NamingContext)));
    }

    public IEnumerable<LdapForestEntry> FilterSuffixes(IEnumerable<LdapForestEntry> domains, IPermissionRules permission)
    {
        var result = new List<LdapForestEntry>();
        foreach (var domain in domains)
        {
            var allowedSuffixes = domain.Suffixes.Where(permission.IsPermitted);
            result.Add(new LdapForestEntry(domain.Schema, allowedSuffixes)); 
        }
        
        return result;
    }
}