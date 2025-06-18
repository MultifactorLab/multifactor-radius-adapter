using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class LdapGroupService : ILdapGroupService
{
    private readonly ILdapGroupLoaderFactory _ldapGroupLoaderFactory;
    private readonly IMembershipCheckerFactory _ldapMembershipCheckerFactory;

    public LdapGroupService(ILdapGroupLoaderFactory ldapGroupLoaderFactory, IMembershipCheckerFactory ldapMembershipCheckerFactory)
    {
        ArgumentNullException.ThrowIfNull(ldapGroupLoaderFactory);
        ArgumentNullException.ThrowIfNull(ldapMembershipCheckerFactory);
        
        _ldapGroupLoaderFactory = ldapGroupLoaderFactory;
        _ldapMembershipCheckerFactory = ldapMembershipCheckerFactory;
    }

    public IReadOnlyList<string> LoadUserGroups(ILdapSchema ldapSchema, ILdapConnection connection, DistinguishedName userName, DistinguishedName? searchBase = null, int limit = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(ldapSchema);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(userName);

        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit));
        var groupLoader = _ldapGroupLoaderFactory.GetGroupLoader(ldapSchema, connection, searchBase ?? ldapSchema.NamingContext);
        var groupDns = groupLoader.GetGroups(userName, pageSize: 20);
        return groupDns.Take(limit).Select(x => x.Components.Deepest.Value).ToList();
    }

    public bool IsMemberOf(ILdapSchema ldapSchema, ILdapConnection connection, DistinguishedName userName, DistinguishedName[] groupNames, DistinguishedName? searchBase = null)
    {
        ArgumentNullException.ThrowIfNull(ldapSchema);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(userName);
        ArgumentNullException.ThrowIfNull(groupNames);
        if (groupNames.Length == 0)
            throw new ArgumentException("No groups", nameof(groupNames));
        
        var membershipChecker = _ldapMembershipCheckerFactory.GetMembershipChecker(ldapSchema, connection, searchBase ?? ldapSchema.NamingContext);
        return membershipChecker.IsMemberOf(userName, groupNames); 
    }
}