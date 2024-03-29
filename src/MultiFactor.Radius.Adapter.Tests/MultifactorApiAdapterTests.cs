﻿using Moq;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using System.Net;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Core.Http;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Configuration.Core;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using Elastic.CommonSchema;
using static MultiFactor.Radius.Adapter.Services.MultiFactorApi.Literals;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;

namespace MultiFactor.Radius.Adapter.Tests
{
    [Trait("Category", "Multifactor API")]
    public class MultifactorApiAdapterTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("Denied")]
        [InlineData("SomeUnexpectedStatus")]
        public async Task CreateSecondFactorRequest_BadStatus_ShouldReturnReject(string status)
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
                .SetPrivacyMode(PrivacyModeDescriptor.None);
            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), clientConfig: client, x =>
            {
                x.UserName = "test_user@multifactor.ru";
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
            });

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(PacketCode.AccessReject, result.Code);
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

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret").SetPrivacyMode(PrivacyModeDescriptor.None);
            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), client, x =>
            {
                x.UserName = "test_user@multifactor.ru";
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
            });

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(PacketCode.AccessAccept, result.Code);
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
                        Status = RadiusCode.Denied
                    });
                builder.Services.ReplaceService(api.Object);

                var cache = new Mock<IAuthenticatedClientCache>();
                cache.Setup(x => x.TryHitCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IClientConfiguration>())).Returns(false);
                builder.Services.ReplaceService(cache.Object);
            });

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret").SetPrivacyMode(PrivacyModeDescriptor.None);
            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), client, x =>
            {
                x.UserName = "test_user@multifactor.ru";
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
            });

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(PacketCode.AccessReject, result.Code);
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
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .SetBypassSecondFactorWhenApiUnreachable(false);
            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), client, x =>
            {
                x.UserName = "test_user@multifactor.ru";
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
                x.RequestPacket.AddAttribute("Calling-Station-Id", "192.168.1.1");
            });

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(PacketCode.AccessReject, result.Code);
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
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .SetBypassSecondFactorWhenApiUnreachable(true);
            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), client, x =>
            {
                x.UserName = "test_user@multifactor.ru";
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
                x.RequestPacket.AddAttribute("Calling-Station-Id", "192.168.1.1");
            });

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(PacketCode.AccessAccept, result.Code);
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
                        Status = RadiusCode.Granted
                    });
                builder.Services.ReplaceService(api.Object);
            });

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .SetUseAttributeAsIdentity("some_attr_name");
            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), client, x =>
            {
                x.UserName = "test_user@multifactor.ru";
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
            });

            var profile = new LdapProfile(LdapIdentity.ParseUser("test_user@multifactor.ru"), new LdapAttributes(), Array.Empty<string>(), "some_attr_value");
            context.SetProfile(profile);

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(PacketCode.AccessAccept, result.Code);
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
                        Status = RadiusCode.Granted
                    });
                builder.Services.ReplaceService(api.Object);
            });

            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .SetUseAttributeAsIdentity("some_attr_name");
            var context = host.CreateContext(RadiusPacketFactory.AccessRequest(), client, x =>
            {
                x.UserName = "test_user@multifactor.ru";
                x.RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636);
            });

            var profile = new LdapProfile(LdapIdentity.ParseUser("test_user@multifactor.ru"), new LdapAttributes(), Array.Empty<string>(), null);
            context.SetProfile(profile);

            var adapter = host.Service<MultifactorApiAdapter>();
            var result = await adapter.CreateSecondFactorRequestAsync(context);

            Assert.Equal(PacketCode.AccessReject, result.Code);
        }
    }
}