using Moq;
using MultiFactor.Radius.Adapter.Server;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;
using System.Net;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Core.Http;
using MultiFactor.Radius.Adapter.Services;
using Serilog;
using MultiFactor.Radius.Adapter.Configuration;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Configuration.Features.PrivacyModeFeature;

namespace MultiFactor.Radius.Adapter.Tests
{
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
            var client = ClientConfiguration.CreateBuilder("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .Build();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };
            var cache = new Mock<IAuthenticatedClientCache>();
            var adapter = new Mock<IHttpClientAdapter>();
            var response = new MultiFactorApiResponse<MultiFactorAccessRequest> 
            {
                Success = true,
                Model = new MultiFactorAccessRequest
                {
                    Status = status
                }
            };
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<MultiFactorAccessRequest>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ReturnsAsync(response);

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessReject, result);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_CachedUser_ShouldReturnAccept()
        {
            var client = ClientConfiguration.CreateBuilder("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .Build();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };
            var cache = new Mock<IAuthenticatedClientCache>();
            cache.Setup(x => x.TryHitCache(It.IsAny<string>(), It.IsAny<string>(), client)).Returns(true);

            var adapter = new Mock<IHttpClientAdapter>();
            var response = new MultiFactorApiResponse<MultiFactorAccessRequest>
            {
                Success = false,
                Model = new MultiFactorAccessRequest
                {
                    Status = Literals.RadiusCode.Denied
                }
            };
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<MultiFactorAccessRequest>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ReturnsAsync(response);

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessAccept, result);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_NotCachedUser_ShouldReturnReject()
        {
            var client = ClientConfiguration.CreateBuilder("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .Build();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };
            var cache = new Mock<IAuthenticatedClientCache>();
            cache.Setup(x => x.TryHitCache(It.IsAny<string>(), It.IsAny<string>(), client)).Returns(false);

            var adapter = new Mock<IHttpClientAdapter>();
            var response = new MultiFactorApiResponse<MultiFactorAccessRequest>
            {
                Success = false,
                Model = new MultiFactorAccessRequest
                {
                    Status = Literals.RadiusCode.Denied
                }
            };
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<MultiFactorAccessRequest>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ReturnsAsync(response);

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessReject, result);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_ThrowsMultifactorApiUnreachableExceptionAndBypassIsNotConfigured_ShouldReturnReject()
        {
            var client = ClientConfiguration.CreateBuilder("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .SetBypassSecondFactorWhenApiUnreachable(false)
                .Build();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };
            var cache = new Mock<IAuthenticatedClientCache>();

            var adapter = new Mock<IHttpClientAdapter>();
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<MultiFactorAccessRequest>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ThrowsAsync(new MultifactorApiUnreachableException());

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessReject, result);
        }
        
        [Fact]
        public async Task CreateSecondFactorRequest_ThrowsMultifactorApiUnreachableExceptionAndBypassIsConfigured_ShouldReturnAccept()
        {
            var client = ClientConfiguration.CreateBuilder("cli_config", "rds", AuthenticationSource.None, "key", "secret")
                .SetPrivacyMode(PrivacyModeDescriptor.None)
                .Build();
            var responseSender = new Mock<IRadiusResponseSender>();
            var context = new RadiusContext(client, responseSender.Object, new Mock<IServiceProvider>().Object)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 636),
                RequestPacket = RadiusPacketFactory.AccessRequest()
            };
            var cache = new Mock<IAuthenticatedClientCache>();

            var adapter = new Mock<IHttpClientAdapter>();
            adapter.Setup(x => x.PostAsync<MultiFactorApiResponse<MultiFactorAccessRequest>>("access/requests/ra", It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
                .ThrowsAsync(new MultifactorApiUnreachableException());

            var api = new MultiFactorApiClient(cache.Object, new Mock<ILogger>().Object, adapter.Object);
            var result = await api.CreateSecondFactorRequest(context);

            Assert.Equal(PacketCode.AccessAccept, result);
        }
    }
}
