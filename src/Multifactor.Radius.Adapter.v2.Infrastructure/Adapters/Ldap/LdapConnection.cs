using System.Collections.ObjectModel;
using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Entry;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Core.Ldap.Name;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Ldap;

public class LdapConnection : ILdapConnection
{
    private readonly ILdapConnection _ldapConnection;
    
    public LdapConnection(ILdapConnection connection)
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