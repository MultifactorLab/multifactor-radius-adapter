using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class LoadUserGroupsRequest
{
    public ILdapSchema LdapSchema { get; }
    public ILdapConnection LdapConnection { get; }
    public DistinguishedName UserName { get; }
    public DistinguishedName? SearchBase { get; }
    public int Limit { get; }

    public LoadUserGroupsRequest(ILdapSchema ldapSchema, ILdapConnection ldapConnection, DistinguishedName userName, DistinguishedName? searchBase, int limit = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(ldapSchema);
        ArgumentNullException.ThrowIfNull(ldapConnection);
        ArgumentNullException.ThrowIfNull(userName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit, nameof(limit));
        
        LdapSchema = ldapSchema;
        LdapConnection = ldapConnection;
        UserName = userName;
        SearchBase = searchBase;
        Limit = limit;
    }
}