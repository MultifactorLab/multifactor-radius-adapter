using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap;

public static class LdapNamesUtils
{
    /// <summary>
    /// Converts domain.local to DC=domain,DC=local
    /// </summary>
    public static DistinguishedName FqdnToDn(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

        var portIndex = name.IndexOf(':');
        if (portIndex > 0)
        {
            name = name[..portIndex];
        }

        var domains = name.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
        var dnParts = domains.Select(p => $"DC={p}").ToArray();
        var dn = string.Join(",", dnParts);
        return new DistinguishedName(dn);
    }
    
    public static string DnToFqdn(DistinguishedName name)
    {
        var ncs = name.Components.Reverse();
        return string.Join(".", ncs.Select(x => x.Value));
    }
}