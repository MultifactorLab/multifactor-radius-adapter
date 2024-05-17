﻿using MultiFactor.Radius.Adapter.Configuration;

namespace MultiFactor.Radius.Adapter.Tests
{
    [Trait("Categoty", "Configuration Validation")]
    public class ServiceConfigurationExtensionsTests
    {
        [Theory]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.None)]
        public void Validate_ShouldSuccess(AuthenticationSource src)
        {
            var cli = new ClientConfiguration("cliname", "secret", src, "key", "secret");
            var config = new ServiceConfiguration().AddClient("cliname", cli);

            ServiceConfigurationExtensions.Validate(config);
        }
        
        [Theory]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Ldap)]
        public void Validate_AdOrLDAP_ShouldThrow(AuthenticationSource src)
        {
            var cli = new ClientConfiguration("cliname", "secret", src, "key", "secret");
            var config = new ServiceConfiguration().AddClient("cliname", cli);

            var ex = Assert.Throws<Exception>(() => ServiceConfigurationExtensions.Validate(config));

            Assert.Equal("Configuration error: 'service-account-user' and 'service-account-password' elements not found. Please check configuration of client 'cliname'.", ex.Message);
        }
        
        [Theory]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Ldap)]
        [InlineData(AuthenticationSource.Radius)]
        [InlineData(AuthenticationSource.None)]
        public void Validate_CheckMembership_ShouldThrow(AuthenticationSource src)
        {
            var cli = new ClientConfiguration("cliname", "secret", src, "key", "secret")
                .SetActiveDirectoryDomain("domain")
                .AddActiveDirectory2FaGroup("group");
            var config = new ServiceConfiguration().AddClient("cliname", cli);

            var ex = Assert.Throws<Exception>(() => ServiceConfigurationExtensions.Validate(config));

            Assert.Equal("Configuration error: 'service-account-user' and 'service-account-password' elements not found. Please check configuration of client 'cliname'.", ex.Message);
        }
    }
}
