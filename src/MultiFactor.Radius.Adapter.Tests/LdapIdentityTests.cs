using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class LdapIdentityTests
    {
        [Fact]
        public void ParseUser_Dn()
        {
            var identity = LdapIdentity.ParseUser("CN=User Name,CN=Users,DC=domain,DC=local");

            Assert.Equal(IdentityType.DistinguishedName, identity.Type);
            Assert.Equal("CN=User Name,CN=Users,DC=domain,DC=local".ToLower(), identity.Name);
        }
    }
}
