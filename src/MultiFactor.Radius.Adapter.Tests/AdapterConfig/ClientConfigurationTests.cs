using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig
{
    public class ClientConfigurationTests
    {
        [Theory]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void ShouldLoadUserGroups_HasActiveDirectoryGroups_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .AddActiveDirectoryGroups(new[] { "group" });

            Assert.True(client.ShouldLoadUserGroups);
        }
        
        [Theory]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void ShouldLoadUserGroups_HasActiveDirectory2FaGroup_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .AddActiveDirectory2FaGroup("group");

            Assert.True(client.ShouldLoadUserGroups);
        }
        
        [Theory]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void ShouldLoadUserGroups_HasActiveDirectory2FaBypassGroup_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .AddActiveDirectory2FaBypassGroup("group");

            Assert.True(client.ShouldLoadUserGroups);
        }
        
        [Theory]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void ShouldLoadUserGroups_HasPhoneAttributes_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .AddPhoneAttribute("attr");

            Assert.True(client.ShouldLoadUserGroups);
        }
        
        [Theory]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void ShouldLoadUserGroups_HasRadiusReplyAttributesLdap_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .AddRadiusReplyAttribute("attr", new[] { new RadiusReplyAttributeValue("ldapattr") });

            Assert.True(client.ShouldLoadUserGroups);
        }
        
        [Theory]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void ShouldLoadUserGroups_HasRadiusReplyAttributesMemberOf_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .AddRadiusReplyAttribute("attr", new[] { new RadiusReplyAttributeValue("memberof") });

            Assert.True(client.ShouldLoadUserGroups);
        }
        
        [Theory]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void ShouldLoadUserGroups_HasRadiusReplyAttributesUserGroupCond_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .AddRadiusReplyAttribute("attr", new[] { new RadiusReplyAttributeValue("value", "UserGroup=group") });

            Assert.True(client.ShouldLoadUserGroups);
        }
        
        [Theory]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void ShouldLoadUserGroups_NoGroups_ShouldReturnFalse(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret");

            Assert.False(client.ShouldLoadUserGroups);
        }
        
        [Theory]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void ShouldLoadUserProfile_NoDomain_ShouldReturnFalse(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret");
            
            Assert.False(client.ShouldLoadUserProfile);
        }
        
        [Theory]
        [InlineData(AuthenticationSource.None)]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.Ldap)]
        public void ShouldLoadUserProfile_HasDomain_ShouldReturnTrue(AuthenticationSource src)
        {
            var client = new ClientConfiguration("custom", "shared_secret", src, "key", "secret")
                .SetActiveDirectoryDomain("domain");
            
            Assert.True(client.ShouldLoadUserProfile);
        }
    }
}
