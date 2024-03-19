using System;

namespace MultiFactor.Radius.Adapter.Configuration
{
    [Flags]
    public enum LdapCatalogType
    {
        ActiveDirectory = 0,
        OpenLdap = 1,
        FreeIpa = 2,
        Samba = 4
    }
}
