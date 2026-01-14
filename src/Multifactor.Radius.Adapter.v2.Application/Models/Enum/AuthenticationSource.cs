namespace Multifactor.Radius.Adapter.v2.Application.Models.Enum
{
    [Flags]
    public enum AuthenticationSource
    {
        None = 0,
        Radius = 1,
        Ldap = 2
    }
}
