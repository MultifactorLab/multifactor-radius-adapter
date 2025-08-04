using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

/// <summary>
/// Wrap LdapSchemaLoader from MF Core library
/// </summary>
public class LdapSchemaLoaderWrapper : ILdapSchemeLoaderWrapper
{
    private readonly LdapSchemaLoader _ldapSchemaLoader;
    
    public LdapSchemaLoaderWrapper(LdapSchemaLoader ldapSchemaLoader)
    {
        _ldapSchemaLoader = ldapSchemaLoader;
    }
    
    public ILdapSchema? Load(LdapConnectionOptions connectionOptions)
    {
        ArgumentNullException.ThrowIfNull(connectionOptions);
        
        return _ldapSchemaLoader.Load(connectionOptions);
    }
}