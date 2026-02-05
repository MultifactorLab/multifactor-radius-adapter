using Multifactor.Core.Ldap.Attributes;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor.BindNameFormat;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.FirstFactor.BindNameFormat
{
    public class ActiveDirectoryFormatterTests
    {
        private readonly ActiveDirectoryFormatter _formatter;

        public ActiveDirectoryFormatterTests()
        {
            _formatter = new ActiveDirectoryFormatter();
        }

        [Fact]
        public void LdapImplementation_ShouldBeActiveDirectory()
        {
            // Act & Assert
            Assert.Equal(LdapImplementation.ActiveDirectory, _formatter.LdapImplementation);
        }

        [Theory]
        [InlineData("user@domain.com")] // UPN
        [InlineData("DOMAIN\\user")] // NetBIOS
        [InlineData("user")] // sAMAccountName
        [InlineData("CN=User,OU=Users,DC=domain,DC=com")] // DN
        public void FormatName_ShouldReturnOriginalName(string userName)
        {
            // Arrange
            var profile = new MockLdapProfile();

            // Act
            var result = _formatter.FormatName(userName, profile);

            // Assert
            Assert.Equal(userName, result);
        }

        [Fact]
        public void FormatName_ShouldHandleNullProfile()
        {
            // Arrange
            var userName = "testuser";

            // Act
            var result = _formatter.FormatName(userName, null);

            // Assert
            Assert.Equal(userName, result);
        }

        private class MockLdapProfile : ILdapProfile
        {
            public DistinguishedName Dn { get; }
            public string? Phone { get; }
            public string? Email { get; }
            public string? DisplayName { get; }
            public IReadOnlyCollection<DistinguishedName> MemberOf { get; }
            public IReadOnlyCollection<LdapAttribute> Attributes { get; }
        }
    }
}