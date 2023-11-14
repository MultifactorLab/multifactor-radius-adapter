using MultiFactor.Radius.Adapter.Configuration;
using System;

namespace MultiFactor.Radius.Adapter.Services.Ldap;

internal static class LdapNamesFactory
{
    public static LdapNames CreateLdapNames(AuthenticationSource source)
    {
        switch (source)
        {
            case AuthenticationSource.ActiveDirectory:
            case AuthenticationSource.Radius:
            case AuthenticationSource.None:
                return new LdapNames(LdapServerType.ActiveDirectory);
            case AuthenticationSource.Ldap:
                return new LdapNames(LdapServerType.Generic);
            default:
                throw new NotImplementedException(source.ToString());
        }
    }
}
