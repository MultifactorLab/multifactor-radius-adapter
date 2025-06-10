using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Http;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit.MultifactorApi;

public class MultifactorApiTests
{
    [Fact]
    public async Task SendRequest_EmptyPayload_ShouldThrowException()
    {
        var clientMock = new Mock<IHttpClient>();
        var api = new Services.MultifactorApi.MultifactorApi(clientMock.Object, NullLogger<Services.MultifactorApi.MultifactorApi>.Instance);
        await Assert.ThrowsAsync<ArgumentNullException>(() => api.CreateAccessRequest(null, new ApiCredential("key", "secret")));
    }
    
    [Fact]
    public async Task SendRequest_EmptyApiCredential_ShouldThrowException()
    {
        var clientMock = new Mock<IHttpClient>();
        var api = new Services.MultifactorApi.MultifactorApi(clientMock.Object, NullLogger<Services.MultifactorApi.MultifactorApi>.Instance);
        await Assert.ThrowsAsync<ArgumentNullException>(() => api.CreateAccessRequest(new AccessRequest(), null));
    }
    
    [Fact]
    public async Task SendRequest_EmptyHttpResponse_ShouldDenied()
    {
        var clientMock = new Mock<IHttpClient>();
        
        clientMock
            .Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .ReturnsAsync(() => null);
        
        var api = new Services.MultifactorApi.MultifactorApi(clientMock.Object, NullLogger<Services.MultifactorApi.MultifactorApi>.Instance);
        var response = await api.CreateAccessRequest(new AccessRequest(), new ApiCredential("key", "secret"));
        Assert.NotNull(response);
        Assert.Equal(RequestStatus.Denied, response.Status);
    }
    
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendRequest_ShouldReturnResponse(bool success)
    {
        var clientMock = new Mock<IHttpClient>();
        
        clientMock
            .Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .ReturnsAsync(() => new MultiFactorApiResponse<AccessRequestResponse>() { Success = success, Model = new AccessRequestResponse() {Status = RequestStatus.Granted} } );
        
        var api = new Services.MultifactorApi.MultifactorApi(clientMock.Object, NullLogger<Services.MultifactorApi.MultifactorApi>.Instance);
        var response = await api.CreateAccessRequest(new AccessRequest(), new ApiCredential("key", "secret"));
        Assert.NotNull(response);
        Assert.Equal(RequestStatus.Granted, response.Status);
    }


    [Fact]
    public async Task SendRequest_HttpRequestException_ShouldMultifactorApiUnreachableException()
    {
        var clientMock = new Mock<IHttpClient>();
        
        clientMock
            .Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Throws(new HttpRequestException());
        
        var api = new Services.MultifactorApi.MultifactorApi(clientMock.Object, NullLogger<Services.MultifactorApi.MultifactorApi>.Instance);
        await Assert.ThrowsAsync<MultifactorApiUnreachableException>(() => api.CreateAccessRequest(new AccessRequest(), new ApiCredential("key", "secret")));
    }
    
    [Fact]
    public async Task SendRequest_TooManyRequests_ShouldReturnDenied()
    {
        var clientMock = new Mock<IHttpClient>();
        
        clientMock
            .Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Throws(new HttpRequestException(string.Empty, null, HttpStatusCode.TooManyRequests));
        
        var api = new Services.MultifactorApi.MultifactorApi(clientMock.Object, NullLogger<Services.MultifactorApi.MultifactorApi>.Instance);
        var response = await api.CreateAccessRequest(new AccessRequest(), new ApiCredential("key", "secret"));
        Assert.NotNull(response);
        Assert.Equal(RequestStatus.Denied, response.Status);
    }
    
    [Fact]
    public async Task SendRequest_TaskCanceledException_ShouldMultifactorApiUnreachableException()
    {
        var clientMock = new Mock<IHttpClient>();
        
        clientMock
            .Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Throws(new TaskCanceledException());
        
        var api = new Services.MultifactorApi.MultifactorApi(clientMock.Object, NullLogger<Services.MultifactorApi.MultifactorApi>.Instance);
        await Assert.ThrowsAsync<MultifactorApiUnreachableException>(() => api.CreateAccessRequest(new AccessRequest(), new ApiCredential("key", "secret")));
    }
    
    [Fact]
    public async Task SendRequest_Exception_ShouldMultifactorApiUnreachableException()
    {
        var clientMock = new Mock<IHttpClient>();
        
        clientMock
            .Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestResponse>>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Throws(new Exception());
        
        var api = new Services.MultifactorApi.MultifactorApi(clientMock.Object, NullLogger<Services.MultifactorApi.MultifactorApi>.Instance);
        await Assert.ThrowsAsync<MultifactorApiUnreachableException>(() => api.CreateAccessRequest(new AccessRequest(), new ApiCredential("key", "secret")));
    }
}