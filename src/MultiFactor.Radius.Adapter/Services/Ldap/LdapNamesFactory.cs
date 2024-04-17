using MultiFactor.Radius.Adapter.Configuration;
using System;

namespace MultiFactor.Radius.Adapter.Services.Ldap;

internal static class LdapNamesFactory
{
    public static LdapNames CreateLdapNames(AuthenticationSource source)
    {
        return source switch
        {
            AuthenticationSource.ActiveDirectory 
                or AuthenticationSource.Radius 
                or AuthenticationSource.None => new LdapNames(LdapServerType.ActiveDirectory),
            AuthenticationSource.Ldap => new LdapNames(LdapServerType.Generic),
            _ => throw new NotImplementedException(source.ToString()),
        };
    }
}
