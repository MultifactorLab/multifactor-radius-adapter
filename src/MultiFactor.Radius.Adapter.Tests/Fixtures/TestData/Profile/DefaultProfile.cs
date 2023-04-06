using MultiFactor.Radius.Adapter.Services.Ldap;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.TestData.Profile
{
    internal class DefaultProfile : TheoryData<LdapProfile>
    {
        public DefaultProfile()
        {
            Add(new LdapProfile
            {
                DisplayName = "User Name",
                DistinguishedName = "CN=User Name,CN=Users,DC=domain,DC=local",
                BaseDn = LdapIdentity.BaseDn("CN=User Name,CN=Users,DC=domain,DC=local"),
                Email = "username@post.org",
                LdapAttrs = new Dictionary<string, object>
                {
                    { "sAMAccountName", "user.name" },
                    { "userPrincipalName", "user.name@domain.local" },
                },
                MemberOf = new List<string> { "Users" },
                Phone = "",
                Upn = "user.name@domain.local"
            });
        }
    }
}
