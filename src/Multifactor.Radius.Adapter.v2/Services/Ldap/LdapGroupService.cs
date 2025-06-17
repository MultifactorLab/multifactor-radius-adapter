using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class LdapGroupService : ILdapGroupService
{
    private readonly ILdapGroupLoader _loader;

    public LdapGroupService(ILdapSchema ldapSchema, ILdapConnection connection, ILdapGroupLoaderFactory ldapGroupLoaderFactory)
    {
        ArgumentNullException.ThrowIfNull(ldapSchema);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(ldapGroupLoaderFactory);
        
        _loader = ldapGroupLoaderFactory.GetGroupLoader(ldapSchema, connection, ldapSchema.NamingContext);
    }

    public IReadOnlyList<string> LoadUserGroups(DistinguishedName userName, int limit = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(userName);
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit));
        
        var groupDns = _loader.GetGroups(userName, pageSize: 20);
        return groupDns.Select(x => x.Components.Deepest.Value).Take(limit).ToList();
    }
}