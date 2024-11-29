using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using MultiFactor.Radius.Adapter.Infrastructure.Http;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;

namespace MultiFactor.Radius.Adapter.Tests;

public class MultifactorApiClientTests
{
    [Fact]
    public async Task CreateRequest_TooManyRequestsResponse_ShouldReturnReject()
    {
        var exception = new HttpRequestException("Too Many Request", new Exception(), HttpStatusCode.TooManyRequests);
        var httpClientAdapterMock = new Mock<IHttpClientAdapter>();
        httpClientAdapterMock
            .Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestDto>>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<Dictionary<string,string>>()))
            .Throws(exception);
        var logger = new Mock<ILogger<MultifactorApiClient>>();
        var apiClient = new MultifactorApiClient(logger.Object, httpClientAdapterMock.Object);
        
        var response = await apiClient.CreateRequestAsync(new CreateRequestDto(), new BasicAuthHeaderValue("test", "test"));
        
        Assert.Equal(RequestStatus.Denied, response.Status);
    }
    
    [Fact]
    public async Task CreateChallenge_TooManyRequestsResponse_ShouldReturnReject()
    {
        var exception = new HttpRequestException("Too Many Request", new Exception(), HttpStatusCode.TooManyRequests);
        var httpClientAdapterMock = new Mock<IHttpClientAdapter>();
        httpClientAdapterMock
            .Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestDto>>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<Dictionary<string,string>>()))
            .Throws(exception);
        var logger = new Mock<ILogger<MultifactorApiClient>>();
        var apiClient = new MultifactorApiClient(logger.Object, httpClientAdapterMock.Object);
        
        var response = await apiClient.ChallengeAsync(new ChallengeDto(), new BasicAuthHeaderValue("test", "test"));
        
        Assert.Equal(RequestStatus.Denied, response.Status);
    }
    
    [Fact]
    public async Task CreateRequest_UnsuccessfulCodeExceptTooManyRequest_ShouldThrowMultifactorApiUnreachableException()
    {
        var exception = new HttpRequestException("Error", new Exception(), HttpStatusCode.Forbidden);
        var httpClientAdapterMock = new Mock<IHttpClientAdapter>();
        httpClientAdapterMock
            .Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestDto>>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<Dictionary<string,string>>()))
            .Throws(exception);
        var logger = new Mock<ILogger<MultifactorApiClient>>();
        var apiClient = new MultifactorApiClient(logger.Object, httpClientAdapterMock.Object);

        await Assert.ThrowsAsync<MultifactorApiUnreachableException>(async () =>
            await apiClient.CreateRequestAsync(new CreateRequestDto(), new BasicAuthHeaderValue("test", "test")));
    }
    
    [Fact]
    public async Task CreateChallenge_UnsuccessfulCodeExceptTooManyRequest_ShouldThrowMultifactorApiUnreachableException()
    {
        var exception = new HttpRequestException("Error", new Exception(), HttpStatusCode.Forbidden);
        var httpClientAdapterMock = new Mock<IHttpClientAdapter>();
        httpClientAdapterMock
            .Setup(x => x.PostAsync<MultiFactorApiResponse<AccessRequestDto>>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<Dictionary<string,string>>()))
            .Throws(exception);
        var logger = new Mock<ILogger<MultifactorApiClient>>();
        var apiClient = new MultifactorApiClient(logger.Object, httpClientAdapterMock.Object);

        await Assert.ThrowsAsync<MultifactorApiUnreachableException>(async () =>
            await apiClient.ChallengeAsync(new ChallengeDto(), new BasicAuthHeaderValue("test", "test")));
    }
}