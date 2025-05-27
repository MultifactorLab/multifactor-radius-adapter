using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class LdapProfileLoader : ILdapProfileLoader
{
    private readonly ILdapConnection _ldapConnection;
    private readonly ILdapSchema _ldapSchema;
    private readonly DistinguishedName _searchBase;

    public LdapProfileLoader(DistinguishedName searchBase, ILdapConnection ldapConnection, ILdapSchema ldapSchema)
    {
        Throw.IfNull(ldapConnection, nameof(ldapConnection));
        Throw.IfNull(ldapSchema, nameof(ldapSchema));

        _ldapConnection = ldapConnection;
        _ldapSchema = ldapSchema;
        _searchBase = searchBase;
    }
    
    public ILdapProfile? LoadLdapProfile(
        string filter,
        SearchScope scope = SearchScope.Subtree,
        LdapAttributeName[]? attributeNames = null)
    {
        Throw.IfNullOrWhiteSpace(filter, nameof(filter));
        var result = _ldapConnection.Find(_searchBase, filter, scope, attributes: attributeNames ?? []);
        var entry = result.FirstOrDefault();
        return entry is null ? null : new LdapProfile(entry, _ldapSchema);
    }
}