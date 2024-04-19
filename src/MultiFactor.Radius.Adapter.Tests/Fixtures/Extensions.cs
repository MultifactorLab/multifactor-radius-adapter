using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures
{
    internal static class Extensions
    {
        public static LdapIdentity ExtractUpnBasedUser(this LdapProfile profile)
        {
            if (profile is null) throw new ArgumentNullException(nameof(profile));
            return LdapIdentity.ParseUser(profile.Upn);
        }
    }
}
