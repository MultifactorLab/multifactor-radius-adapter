using Moq;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using System.Net;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Core.Http;
using MultiFactor.Radius.Adapter.Services;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Services.Ldap.ProfileLoading;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;
using MultiFactor.Radius.Adapter.Framework.Context;
using MultiFactor.Radius.Adapter.Configuration.Core;

namespace MultiFactor.Radius.Adapter.Tests
{
    [Trait("Category", "Multifactor API")]
    public class MultiFactorApiClientTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("Denied")]
        [InlineData("SomeUnexpectedStatus")]
        public async Task CreateSecondFactorRequest_ShouldReturnReject(string status)
        {
            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None);
            var context = new RadiusContext(client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                UserName = "test_user@multifactor.ru",
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };
            var cache = new Mock<IAuthenticatedClientCache>();
            var adapter = new Mock<IHttpClientAdapter>();
            var response = new MultiFactorApiResponse<AccessRequestDto> 
            {
                Success = true,
                Model = new AccessRequestDto
                {
                    Status = status
                }
            };
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestDto>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ReturnsAsync(response);

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger<MultiFactorApiClient>>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessReject, result);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_CachedUser_ShouldReturnAccept()
        {
            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret").SetPrivacyMode(PrivacyModeDescriptor.None);
            var context = new RadiusContext(client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                UserName = "test_user@multifactor.ru",
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };
            var cache = new Mock<IAuthenticatedClientCache>();
            cache.Setup(x => x.TryHitCache(It.IsAny<string>(), It.IsAny<string>(), client)).Returns(true);

            var adapter = new Mock<IHttpClientAdapter>();
            var response = new MultiFactorApiResponse<AccessRequestDto>
            {
                Success = false,
                Model = new AccessRequestDto
                {
                    Status = Literals.RadiusCode.Denied
                }
            };
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestDto>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ReturnsAsync(response);

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger<MultiFactorApiClient>>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessAccept, result);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_NotCachedUser_ShouldReturnReject()
        {
            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None);
            var context = new RadiusContext(client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                UserName = "test_user@multifactor.ru",
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };
            var cache = new Mock<IAuthenticatedClientCache>();
            cache.Setup(x => x.TryHitCache(It.IsAny<string>(), It.IsAny<string>(), client)).Returns(false);

            var adapter = new Mock<IHttpClientAdapter>();
            var response = new MultiFactorApiResponse<AccessRequestDto>
            {
                Success = false,
                Model = new AccessRequestDto
                {
                    Status = Literals.RadiusCode.Denied
                }
            };
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestDto>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ReturnsAsync(response);

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger<MultiFactorApiClient>>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessReject, result);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_ThrowsMultifactorApiUnreachableExceptionAndBypassIsNotConfigured_ShouldReturnReject()
        {
            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .SetBypassSecondFactorWhenApiUnreachable(false);
            var context = new RadiusContext(client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                UserName = "test_user@multifactor.ru",
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };
            var cache = new Mock<IAuthenticatedClientCache>();

            var adapter = new Mock<IHttpClientAdapter>();
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestDto>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ThrowsAsync(new MultifactorApiUnreachableException());

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger<MultiFactorApiClient>>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessReject, result);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_ThrowsMultifactorApiUnreachableExceptionAndBypassIsConfigured_ShouldReturnAccept()
        {
            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None);
            var context = new RadiusContext(client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                UserName = "test_user@multifactor.ru",
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };
            var cache = new Mock<IAuthenticatedClientCache>();

            var adapter = new Mock<IHttpClientAdapter>();
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestDto>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ThrowsAsync(new MultifactorApiUnreachableException());

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger<MultiFactorApiClient>>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessAccept, result);
        }

        [Fact]
        public async Task CreateSecondFactorRequest_UseAttributeAsIdentityEnable_ShouldReturnAccept()
        {
            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .SetUseAttributeAsIdentity("some_attr_name");
            var context = new RadiusContext(client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest(),  
            };
            var profile = LdapProfile.CreateBuilder(LdapIdentity.ParseUser("test_user@multifactor.ru"), "dn").SetIdentityAttribute("some_attr_value").Build();
            context.SetProfile(profile);
            var cache = new Mock<IAuthenticatedClientCache>();

            Assert.Equal("some_attr_value", context.SecondFactorIdentity);

            var adapter = new Mock<IHttpClientAdapter>();
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestDto>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ThrowsAsync(new MultifactorApiUnreachableException());

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger<MultiFactorApiClient>>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessAccept, result);
        }

        [Fact]
        public async Task CreateSecondFactorRequest_UseAttributeAsIdentityEnableButEmpty_ShouldReturnReject()
        {
            var client = new ClientConfiguration("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .SetUseAttributeAsIdentity("some_attr");
            var context = new RadiusContext(client, new Mock<IUdpClient>().Object, new Mock<IServiceProvider>().Object)
            {
                UserName = "test_user@multifactor.ru",
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest(),
            };
            var cache = new Mock<IAuthenticatedClientCache>();

            var adapter = new Mock<IHttpClientAdapter>();
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestDto>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ThrowsAsync(new MultifactorApiUnreachableException());

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger<MultiFactorApiClient>>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessReject, result);
        }
    }
}
