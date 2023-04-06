using FluentAssertions;
using LdapForNet;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Core.Exceptions;
using MultiFactor.Radius.Adapter.Core.Ldap;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LdapForNet.Native.Native;

namespace MultiFactor.Radius.Adapter.Tests;

public class ProfileLoaderTests
{
    [Fact]
    public async Task Load_NonExistentUser_ShouldThrow()
    {
        var host = TestHostFactory.CreateHost(services =>
        {
            services.RemoveService<IRootConfigurationProvider>().AddSingleton<IRootConfigurationProvider, TestRootConfigProvider>();
            services.RemoveService<IClientConfigurationsProvider>().AddSingleton<IClientConfigurationsProvider, TestClientConfigsProvider>();
            services.Configure<TestConfigProviderOptions>(x =>
            {
                x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
            });
        });

        var adapter = new Mock<ILdapConnectionAdapter>();
        adapter.Setup(x => x.SearchQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<LdapSearchScope>(), It.IsAny<string[]>()))
            .ReturnsAsync(Array.Empty<LdapEntry>());
        adapter.Setup(x => x.WhereAmIAsync())
            .ReturnsAsync(LdapDomain.Parse("dc=domain,dc=local"));

        var config = host.Services.GetRequiredService<IServiceConfiguration>();
        var clientConfig = config.Clients[0];
        var loader = host.Services.GetRequiredService<ProfileLoader>();

        var act = async () => await loader.LoadAsync(clientConfig, adapter.Object, LdapIdentity.ParseUser("some.user@domain.local"));

        await act.Should().ThrowAsync<LdapUserNotFoundException>();
    }
}
