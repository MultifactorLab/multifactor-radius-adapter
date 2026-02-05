using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor.BindNameFormat;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.FirstFactor.BindNameFormat
{
    public class FreeIpaFormatterTests
    {
        private readonly FreeIpaFormatter _formatter;

        public FreeIpaFormatterTests()
        {
            _formatter = new FreeIpaFormatter();
        }

        [Fact]
        public void LdapImplementation_ShouldReturnFreeIPA()
        {
            // Assert
            Assert.Equal(LdapImplementation.FreeIPA, _formatter.LdapImplementation);
        }

        [Fact]
        public void FormatName_ShouldReturnOriginal_WhenUserPrincipalNameFormat()
        {
            // Arrange
            var userName = "user@domain.com";
            var ldapProfile = new MockLdapProfile("cn=user,dc=test");

            // Act
            var result = _formatter.FormatName(userName, ldapProfile);

            // Assert
            Assert.Equal(userName, result);
        }

        [Fact]
        public void FormatName_ShouldReturnOriginal_WhenDistinguishedNameFormat()
        {
            // Arrange
            var userName = "cn=user,ou=users,dc=test";
            var ldapProfile = new MockLdapProfile("cn=user,dc=test");

            // Act
            var result = _formatter.FormatName(userName, ldapProfile);

            // Assert
            Assert.Equal(userName, result);
        }

        [Fact]
        public void FormatName_ShouldReturnLdapProfileDn_WhenSimpleNameFormat()
        {
            // Arrange
            var userName = "simpleuser";
            var ldapProfile = new MockLdapProfile("cn=simpleuser,ou=users,dc=test");

            // Act
            var result = _formatter.FormatName(userName, ldapProfile);

            // Assert
            Assert.Equal("cn=simpleuser,ou=users,dc=test", result);
        }

        [Fact]
        public void FormatName_ShouldReturnLdapProfileDn_WhenNetBiosNameFormat()
        {
            // Arrange
            var userName = "DOMAIN\\user";
            var ldapProfile = new MockLdapProfile("cn=user,ou=users,dc=domain,dc=test");

            // Act
            var result = _formatter.FormatName(userName, ldapProfile);

            // Assert
            Assert.Equal("cn=user,ou=users,dc=domain,dc=test", result);
        }

        [Fact]
        public void FormatName_ShouldReturnLdapProfileDn_WhenEmailFormat()
        {
            // Arrange
            var userName = "user@test.local";
            var ldapProfile = new MockLdapProfile("cn=user,ou=users,dc=test,dc=local");

            // Act
            var result = _formatter.FormatName(userName, ldapProfile);

            // Assert
            Assert.Equal("cn=user,ou=users,dc=test,dc=local", result);
        }

        [Fact]
        public void FormatName_ShouldReturnLdapProfileDn_WhenUnsupportedFormat()
        {
            // Arrange
            var userName = "just-a-string";
            var ldapProfile = new MockLdapProfile("cn=just-a-string,dc=test");

            // Act
            var result = _formatter.FormatName(userName, ldapProfile);

            // Assert
            Assert.Equal("cn=just-a-string,dc=test", result);
        }

        [Fact]
        public void FormatName_ShouldReturnEmptyString_WhenLdapProfileDnIsEmpty()
        {
            // Arrange
            var userName = "user";
            var ldapProfile = new MockLdapProfile("");

            // Act
            var result = _formatter.FormatName(userName, ldapProfile);

            // Assert
            Assert.Equal("", result);
        }

        private class MockLdapProfile : ILdapProfile
        {
            private readonly string _dn;

            public MockLdapProfile(string dn)
            {
                _dn = dn;
            }

            public DistinguishedName Dn => new DistinguishedName(_dn);
            public string? Phone { get; }
            public string? Email { get; }
            public string? DisplayName { get; }
            public IReadOnlyCollection<DistinguishedName> MemberOf { get; }
            public IReadOnlyCollection<LdapAttribute> Attributes { get; }
        }
    }
}