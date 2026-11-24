using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Bypass;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.SecondFactor;
using static Multifactor.Radius.Adapter.v2.Tests.Fixtures.BypassTestData;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.SecondFactor;

public class ApiHttpDenialTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.TooManyRequests, false)]
    [InlineData(HttpStatusCode.Unauthorized, true)]
    [InlineData(HttpStatusCode.Forbidden, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    public async Task DeniesInitialAndChallengeRequestsEvenWhenPolicyAllowsIp(HttpStatusCode status, bool throwException)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient("multifactor-api")).Returns(() =>
            new HttpClient(new StatusHandler(status, throwException)) { BaseAddress = new Uri("https://api.test/") });

        var policy = new ApiUnreachableBypassPolicy(Membership(false).Object, NullLogger<ApiUnreachableBypassPolicy>.Instance);
        var service = new MultifactorApiService(Mock.Of<IAuthenticatedClientCache>(), policy,
            new CreateAccessRequest(factory.Object, NullLogger<ICreateAccessRequest>.Instance),
            new SendChallenge(factory.Object, NullLogger<ISendChallenge>.Instance), NullLogger<MultifactorApiService>.Instance);
        var context = Context(AllowedIp, Configuration("IP_ONLY"));

        var initial = await service.CreateSecondFactorRequestAsync(context, false);
        var challenge = await service.SendChallengeAsync(context, false, "request-id", "123456");

        Assert.Equal(AuthenticationStatus.Reject, initial.Code);
        Assert.Equal(AuthenticationStatus.Reject, challenge.Code);
        factory.Verify(x => x.CreateClient("multifactor-api"), Times.Exactly(2));
    }

    private sealed class StatusHandler(HttpStatusCode status, bool throwException) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (throwException)
                throw new HttpRequestException(status.ToString(), null, status);

            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}
