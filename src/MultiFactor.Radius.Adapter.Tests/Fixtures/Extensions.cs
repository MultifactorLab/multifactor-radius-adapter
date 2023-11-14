using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures
{
    internal static class Extensions
    {
        public static LdapIdentity ExtractUpnBasedUser(this ILdapProfile profile)
        {
            if (profile is null) throw new ArgumentNullException(nameof(profile));
            return LdapIdentity.ParseUser(profile.Upn);
        }
    }
}
