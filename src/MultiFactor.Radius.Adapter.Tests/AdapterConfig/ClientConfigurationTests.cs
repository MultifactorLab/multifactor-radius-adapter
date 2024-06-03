using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Server;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig
{
    public class ClientConfigurationTests
    {
        [Theory]
        [Trait("Category", "CheckMembership")]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void CheckMembership_ShouldReturnFalse(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret");
            Assert.False(client.CheckMembership);
        }

        [Theory]
        [Trait("Category", "CheckMembership")]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void CheckMembership_HasActiveDirectoryGroups_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectoryGroups(new[] { "group" });

            Assert.True(client.CheckMembership);
        }
        
        [Theory]
        [Trait("Category", "CheckMembership")]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void CheckMembership_HasActiveDirectory2FaGroup_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaGroup("group");

            Assert.True(client.CheckMembership);
        }
        
        [Theory]
        [Trait("Category", "CheckMembership")]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void CheckMembership_HasActiveDirectory2FaBypassGroup_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddActiveDirectory2FaBypassGroup("group");

            Assert.True(client.CheckMembership);
        }
        
        [Theory]
        [Trait("Category", "CheckMembership")]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void CheckMembership_HasPhoneAttributes_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddPhoneAttribute("attr");

            Assert.True(client.CheckMembership);
        }
        
        [Theory]
        [Trait("Category", "CheckMembership")]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void CheckMembership_HasRadiusReplyAttributesLdap_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddRadiusReplyAttribute("attr", new[] { new RadiusReplyAttributeValue("ldapattr") });

            Assert.True(client.CheckMembership);
        }
        
        [Theory]
        [Trait("Category", "CheckMembership")]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void CheckMembership_HasRadiusReplyAttributesMemberOf_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddRadiusReplyAttribute("attr", new[] { new RadiusReplyAttributeValue("memberof") });

            Assert.True(client.CheckMembership);
        }
        
        [Theory]
        [Trait("Category", "CheckMembership")]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void CheckMembership_HasRadiusReplyAttributesUserGroupCond_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .SetActiveDirectoryDomain("domain.local")
                .AddRadiusReplyAttribute("attr", new[] { new RadiusReplyAttributeValue("value", "UserGroup=group") });

            Assert.True(client.CheckMembership);
        }
    }
}
