using Multifactor.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap;

public static class LdapConnectionStringExtensions
{
    /// <summary>
    /// Copy LDAP schema and port from ldapConnectionString with new host
    /// Required to create the same connection to a new host.
    /// </summary>
    public static LdapConnectionString CopySchemaAndPort(this LdapConnectionString ldapConnectionString, string newHost)
    {
        var initialLdapSchema = ldapConnectionString.Scheme;
        var initialLdapPort = ldapConnectionString.Port;
        return new LdapConnectionString($"{initialLdapSchema}://{newHost}:{initialLdapPort}");
    }
}