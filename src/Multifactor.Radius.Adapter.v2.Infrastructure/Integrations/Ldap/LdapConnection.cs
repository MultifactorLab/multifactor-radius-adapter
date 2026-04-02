using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Connection;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Ldap;

internal sealed class LdapConnection : ILdapConnection
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
}