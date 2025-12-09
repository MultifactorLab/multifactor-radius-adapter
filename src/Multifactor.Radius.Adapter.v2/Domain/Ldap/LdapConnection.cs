using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Entry;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;

namespace Multifactor.Radius.Adapter.v2.Domain.Ldap;

public class LdapConnection : ILdapConnection
{
    private readonly Multifactor.Core.Ldap.Connection.ILdapConnection _ldapConnection;
    
    public LdapConnection(Multifactor.Core.Ldap.Connection.ILdapConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _ldapConnection = connection;
    }

    public void Dispose()
    {
        _ldapConnection.Dispose();
    }

    public DirectoryResponse SendRequest(DirectoryRequest request)
    {
        return _ldapConnection.SendRequest(request);
    }

    public ReadOnlyCollection<LdapEntry> Find(
        DistinguishedName searchBase,
        string filter,
        SearchScope scope,
        PageResultRequestControl? pageControl = null,
        params LdapAttributeName[] attributes)
    {
        return _ldapConnection.Find(searchBase, filter, scope, pageControl, attributes);
    }
}