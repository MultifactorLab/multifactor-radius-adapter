using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices.ChallengeProcessor;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Processor;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.PreAuthPostCheck;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters;
using static Multifactor.Radius.Adapter.v2.Tests.Fixtures.BypassTestData;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.SecondFactor;

public class SecondFactorChallengeTests
{
    [Theory]
    [InlineData(AuthenticationStatus.Accept, AuthenticationStatus.Accept, PacketCode.AccessAccept)]
    [InlineData(AuthenticationStatus.Awaiting, AuthenticationStatus.Accept, PacketCode.AccessAccept)]
    [InlineData(AuthenticationStatus.Awaiting, AuthenticationStatus.Reject, PacketCode.AccessReject)]
    public async Task AllowedBypassCompletesChallengeAndRestoresFirstFactor(
        AuthenticationStatus originalFirstFactor, AuthenticationStatus firstFactorResult, PacketCode expectedCode)
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new ChallengeContextCache(memoryCache, NullLogger<ChallengeContextCache>.Instance);
        var service = Service();
        var membership = Membership(false).Object;
        var processor = new SecondFactorChallengeProcessor(service, membership,
            NullLogger<SecondFactorChallengeProcessor>.Instance, cache);
        var provider = new ChallengeProcessorProvider([processor]);

        var config = Configuration("IP_ONLY");
        var original = Context(AllowedIp, config);
        original.ResponseInformation.State = "request-id";
        original.FirstFactorStatus = originalFirstFactor;
        original.Passphrase = UserPassphrase.Parse("original password", PreAuthMode.None);
        var identifier = processor.AddChallengeContext(original);

        var reply = Context(AllowedIp, config);
        reply.RequestPacket.AddAttributeValue("State", identifier.RequestId);
        reply.RequestPacket.AddAttributeValue("User-Password", "123456");
        reply.Passphrase = UserPassphrase.Parse("123456", PreAuthMode.None);

        await new AccessChallengeStep(provider, NullLogger<AccessChallengeStep>.Instance).ExecuteAsync(reply);
        await new SecondFactorStep(service, provider, membership, NullLogger<SecondFactorStep>.Instance).ExecuteAsync(reply);

        Assert.False(reply.IsTerminated);
        Assert.False(processor.HasChallengeContext(identifier));
        Assert.Same(original.Passphrase, reply.Passphrase);
        Assert.Same(original.LdapProfile, reply.LdapProfile);
        Assert.Equal(originalFirstFactor, reply.FirstFactorStatus);
        Assert.Equal(AuthenticationStatus.Bypass, reply.SecondFactorStatus);
        if (originalFirstFactor == AuthenticationStatus.Awaiting)
        {
            await new PreAuthPostCheckStep(NullLogger<PreAuthPostCheckStep>.Instance).ExecuteAsync(reply);
            Assert.False(reply.IsTerminated);
            var firstFactor = new Mock<IFirstFactorProcessor>();
            firstFactor.Setup(x => x.Execute(reply)).Returns(() =>
            {
                reply.FirstFactorStatus = firstFactorResult;
                return Task.CompletedTask;
            });
            var processors = new Mock<IFirstFactorProcessorProvider>();
            processors.Setup(x => x.GetProcessor(reply.ClientConfiguration.FirstFactorAuthenticationSource)).Returns(firstFactor.Object);
            await new FirstFactorStep(processors.Object, provider, NullLogger<FirstFactorStep>.Instance).ExecuteAsync(reply);
            firstFactor.Verify(x => x.Execute(reply), Times.Once);
        }

        Assert.Equal(expectedCode, AdapterResponseSender.DetermineResponseCode(reply.FirstFactorStatus, reply.SecondFactorStatus));
    }

    [Fact]
    public async Task CompletedSecondFactorRetainsVerifiedStatus()
    {
        var context = Context(AllowedIp, Configuration("IP_ONLY"));
        context.SecondFactorStatus = AuthenticationStatus.Accept;
        var step = new SecondFactorStep(Service(), new ChallengeProcessorProvider([]), Membership(false).Object,
            NullLogger<SecondFactorStep>.Instance);

        await step.ExecuteAsync(context);

        Assert.Equal(AuthenticationStatus.Accept, context.SecondFactorStatus);
    }
}
