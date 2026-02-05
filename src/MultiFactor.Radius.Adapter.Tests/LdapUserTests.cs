using FluentAssertions;
using MultiFactor.Radius.Adapter.Core.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap;

namespace MultiFactor.Radius.Adapter.Tests
{
    [Trait("Category", "Ldap")]
    [Trait("Category", "Ldap User")]
    public class LdapUserTests
    {
        [Theory]
        [InlineData("User.Name", "user.name")]
        [InlineData("user.name", "user.name")]
        [InlineData(" user.name ", "user.name")]
        [InlineData("user.name@domain.local", "user.name@domain.local")]
        [InlineData("CN=User Name,CN=Users,DC=domain,DC=local ", "cn=user name,cn=users,dc=domain,dc=local")]
        public void Parse_ShouldReturnObject(string identity, string expected)
        {
            var user = LdapUser.Parse(identity);

            user.Should().NotBeNull();
            user.Name.Should().NotBeNullOrWhiteSpace();
            user.ToString().Should().Be(expected);
            user.Prefix.Should().BeEmpty();
        }
        
        [Theory]
        [InlineData("User.Name", IdentityType.Uid)]
        [InlineData("user.name@domain.local", IdentityType.UserPrincipalName)]
        [InlineData("CN=User Name,CN=Users,DC=domain,DC=local", IdentityType.DistinguishedName)]
        public void Parse_ShouldReturnCorrectType(string identity, IdentityType expectedType)
        {
            var user = LdapUser.Parse(identity);
            user.Type.Should().Be(expectedType);
        }
        
        [Theory]
        [InlineData("User.Name", "user.name")]
        [InlineData("user.name@domain.local", "user.name@domain.local")]
        [InlineData("CN=User Name,CN=Users,DC=domain,DC=local", "cn=user name,cn=users,dc=domain,dc=local")]
        public void Parse_ShouldReturnCorrectName(string identity, string expectedName)
        {
            var user = LdapUser.Parse(identity);
            user.Name.Should().Be(expectedName);
        }
        
        [Theory]
        [InlineData("DOMAIN\\user.name", "DOMAIN")]
        [InlineData("user.name", "")]
        public void Parse_ShouldReturnPrefix(string identity, string expectedPrefix)
        {
            var user = LdapUser.Parse(identity);
            user.Prefix.Should().Be(expectedPrefix);
        }
    }
}
