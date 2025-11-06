namespace Multifactor.Radius.Adapter.v2.Core.Auth
{
    [Flags]
    public enum AuthenticationSource
    {
        None = 0,
        Radius = 1,
        Ldap = 2
    }
}
