using Moq;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using System.Net;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;
using MultiFactor.Radius.Adapter.Core.Framework.Context;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PrivacyModeFeature;
using MultiFactor.Radius.Adapter.Infrastructure.Http;

namespace MultiFactor.Radius.Adapter.Tests
{
    [Trait("Category", "Multifactor API")]
    public class MultifactorApiAdapterTests
    {
        [Theory]
        [InlineData(RequestStatus.Denied)]
        [InlineData((RequestStatus) 999)]
        public async Task CreateSecondFactorRequest_BadStatus_ShouldReturnReject(RequestStatus status)
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.AddSingleton<MultifactorApiAdapter>();
                builder.Services.ReplaceService<IMultifactorApiAdapter>(prov => prov.GetRequiredService<MultifactorApiAdapter>());

                var api = new Mock<IMultifactorApiClient>();
                api.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>())).ReturnsAsync(new AccessRequestDto
                {
                    Status = status
                });
                builder.Services.ReplaceService(api.Object);
            });
            
            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.Default);
            var packet = RadiusPacketFactory.AccessRequest();
            packet.AddAttribute("User-Name", "test_user@multifactor.ru");
            var context = host.CreateContext(packet, clientConfig: client, x =>
            {
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
            });

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(AuthenticationCode.Reject, result.Code);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_CachedUser_ShouldReturnAccept()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.AddSingleton<MultifactorApiAdapter>();
                builder.Services.ReplaceService<IMultifactorApiAdapter>(prov => prov.GetRequiredService<MultifactorApiAdapter>());

                var api = new Mock<IMultifactorApiClient>();
                builder.Services.ReplaceService(api.Object);
                 
                var cache = new Mock<IAuthenticatedClientCache>();
                cache.Setup(x => x.TryHitCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IClientConfiguration>())).Returns(true);
                builder.Services.ReplaceService(cache.Object);
            });

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret").SetPrivacyMode(PrivacyModeDescriptor.Default);
            var packet = RadiusPacketFactory.AccessRequest();
            packet.AddAttribute("User-Name", "test_user@multifactor.ru");
            var context = host.CreateContext(packet, client, x =>
            {
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
            });

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(AuthenticationCode.Bypass, result.Code);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_NotCachedUser_ShouldReturnReject()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.AddSingleton<MultifactorApiAdapter>();
                builder.Services.ReplaceService<IMultifactorApiAdapter>(prov => prov.GetRequiredService<MultifactorApiAdapter>());

                var api = new Mock<IMultifactorApiClient>();
                api.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
                    .ReturnsAsync(new AccessRequestDto
                    {
                        Status = RequestStatus.Denied
                    });
                builder.Services.ReplaceService(api.Object);

                var cache = new Mock<IAuthenticatedClientCache>();
                cache.Setup(x => x.TryHitCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IClientConfiguration>())).Returns(false);
                builder.Services.ReplaceService(cache.Object);
            });

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret").SetPrivacyMode(PrivacyModeDescriptor.Default);
            var packet = RadiusPacketFactory.AccessRequest();
            packet.AddAttribute("User-Name", "test_user@multifactor.ru");
            var context = host.CreateContext(packet, client, x =>
            {
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
            });

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(AuthenticationCode.Reject, result.Code);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_ThrowsMultifactorApiUnreachableExceptionAndBypassIsNotConfigured_ShouldReturnReject()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.AddSingleton<MultifactorApiAdapter>();
                builder.Services.ReplaceService<IMultifactorApiAdapter>(prov => prov.GetRequiredService<MultifactorApiAdapter>());

                var api = new Mock<IMultifactorApiClient>();
                api.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
                    .Throws<MultifactorApiUnreachableException>();
                builder.Services.ReplaceService(api.Object);
            });

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.Default)
                .SetBypassSecondFactorWhenApiUnreachable(false);
            var packet = RadiusPacketFactory.AccessRequest();
            packet.AddAttribute("User-Name", "test_user@multifactor.ru");
            var context = host.CreateContext(packet, client, x =>
            {
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
                x.RequestPacket.AddAttribute("Calling-Station-Id", "192.168.1.1");
            });

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(AuthenticationCode.Reject, result.Code);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_ThrowsMultifactorApiUnreachableExceptionAndBypassIsConfigured_ShouldReturnAccept()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.AddSingleton<MultifactorApiAdapter>();
                builder.Services.ReplaceService<IMultifactorApiAdapter>(prov => prov.GetRequiredService<MultifactorApiAdapter>());

                var api = new Mock<IMultifactorApiClient>();
                api.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
                    .Throws<MultifactorApiUnreachableException>();
                builder.Services.ReplaceService(api.Object);
            });

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.Default)
                .SetBypassSecondFactorWhenApiUnreachable(true);
            var packet = RadiusPacketFactory.AccessRequest();
            packet.AddAttribute("User-Name", "test_user@multifactor.ru");
            var context = host.CreateContext(packet, client, x =>
            {
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
                x.RequestPacket.AddAttribute("Calling-Station-Id", "192.168.1.1");
            });

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(AuthenticationCode.Bypass, result.Code);
        }

        [Fact]
        public async Task CreateSecondFactorRequest_UseAttributeAsIdentityEnableAndNotEmpty_ShouldReturnAccept()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.AddSingleton<MultifactorApiAdapter>();
                builder.Services.ReplaceService<IMultifactorApiAdapter>(prov => prov.GetRequiredService<MultifactorApiAdapter>());

                var api = new Mock<IMultifactorApiClient>();
                api.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
                    .ReturnsAsync(new AccessRequestDto
                    {
                        Status = RequestStatus.Granted
                    });
                builder.Services.ReplaceService(api.Object);
            });

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.Default)
                .SetUseAttributeAsIdentity("some_attr_name");
            var packet = RadiusPacketFactory.AccessRequest();
            packet.AddAttribute("User-Name", "test_user@multifactor.ru");
            var context = host.CreateContext(packet, client, x =>
            {
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
            });

            var user = LdapIdentity.ParseUser("test_user@multifactor.ru");
            var attrs = new LdapAttributes("CN=test_user,CN=Users,DC=domain,DC=local")
                .Add("some_attr_name", "some_attr_value");
            var profile = new LdapProfile(user, attrs, Array.Empty<string>(), "some_attr_name");
            context.UpdateProfile(profile);

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(AuthenticationCode.Accept, result.Code);
        }

        [Fact]
        public async Task CreateSecondFactorRequest_UseAttributeAsIdentityEnableButEmpty_ShouldReturnReject()
        {
            var host = TestHostFactory.CreateHost(builder =>
            {
                builder.Services.Configure<TestConfigProviderOptions>(x =>
                {
                    x.RootConfigFilePath = TestEnvironment.GetAssetPath("root-minimal-single.config");
                });

                builder.Services.AddSingleton<MultifactorApiAdapter>();
                builder.Services.ReplaceService<IMultifactorApiAdapter>(prov => prov.GetRequiredService<MultifactorApiAdapter>());

                var api = new Mock<IMultifactorApiClient>();
                api.Setup(x => x.CreateRequestAsync(It.IsAny<CreateRequestDto>(), It.IsAny<BasicAuthHeaderValue>()))
                    .ReturnsAsync(new AccessRequestDto
                    {
                        Status = RequestStatus.Granted
                    });
                builder.Services.ReplaceService(api.Object);
            });

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.Default)
                .SetUseAttributeAsIdentity("some_attr_name");
            var packet = RadiusPacketFactory.AccessRequest();
            packet.AddAttribute("User-Name", "test_user@multifactor.ru");
            var context = host.CreateContext(packet, client, x =>
            {
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
            });

            var profile = new LdapProfile(LdapIdentity.ParseUser("test_user@multifactor.ru"), 
                new LdapAttributes("CN=test_user,CN=Users,DC=domain,DC=local"), 
                Array.Empty<string>(), 
                null);
            context.UpdateProfile(profile);

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(AuthenticationCode.Reject, result.Code);
        }
    }
}
