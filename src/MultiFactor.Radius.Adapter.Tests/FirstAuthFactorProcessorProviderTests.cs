﻿using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Server.Pipeline.FirstFactorAuthentication.Processing;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class FirstAuthFactorProcessorProviderTests
    {
        [Theory]
        [InlineData(AuthenticationSource.ActiveDirectory)]
        [InlineData(AuthenticationSource.Ldap)]
        public void Get_ShouldReturnLdap(AuthenticationSource source)
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var prov = host.Service<IFirstFactorAuthenticationProcessorProvider>();
            var getter = prov.GetProcessor(source);

            getter.Should().NotBeNull().And.BeOfType<LdapFirstFactorAuthenticationProcessor>();
        }
        
        [Fact]
        public void Get_ShouldReturnRadius()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });
            });

            var prov = host.Service<IFirstFactorAuthenticationProcessorProvider>();
            var getter = prov.GetProcessor(AuthenticationSource.Radius);

            getter.Should().NotBeNull().And.BeOfType<RadiusFirstFactorAuthenticationProcessor>();
        }
    }
}
