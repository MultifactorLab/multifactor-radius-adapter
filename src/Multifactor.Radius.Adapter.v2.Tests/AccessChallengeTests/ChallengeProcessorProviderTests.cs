using Moq;
using Multifactor.Radius.Adapter.v2.Core.AccessChallenge;

namespace Multifactor.Radius.Adapter.v2.Tests.AccessChallengeTests;

public class ChallengeProcessorProviderTests
{
    [Theory]
    [InlineData(ChallengeType.PasswordChange)]
    [InlineData(ChallengeType.SecondFactor)]
    public void GetProcessor_ByType_ShouldReturnProcessor(ChallengeType challengeType)
    {
        var processor = new Mock<IChallengeProcessor>();
        processor.Setup(x => x.ChallengeType).Returns(challengeType);
        var provider = new ChallengeProcessorProvider([processor.Object]);
        var actual = provider.GetChallengeProcessorByType(challengeType);
        
        Assert.NotNull(actual);
        Assert.Equal(challengeType, actual.ChallengeType);
    }

    [Fact]
    public void GetProcessor_ByChallengeIdentifier_ShouldReturnProcessor()
    {
        var processorMock = new Mock<IChallengeProcessor>();
        var identifier = new ChallengeIdentifier("user", "id");
        processorMock.Setup(x => x.HasChallengeContext(It.IsAny<ChallengeIdentifier>())).Returns(true);
        var processor = processorMock.Object;
        var provider = new ChallengeProcessorProvider([processor]);
        var actual = provider.GetChallengeProcessorByIdentifier(identifier);
        
        Assert.NotNull(actual);
        Assert.Equal(processor, actual);
    }

    [Theory]
    [InlineData(ChallengeType.PasswordChange)]
    [InlineData(ChallengeType.SecondFactor)]
    public void GetProcessor_NoSuchType_ShouldReturnNull(ChallengeType challengeType)
    {
        var processor = new Mock<IChallengeProcessor>();
        processor.Setup(x => x.ChallengeType).Returns(ChallengeType.None);
        var provider = new ChallengeProcessorProvider([processor.Object]);
        var actual = provider.GetChallengeProcessorByType(challengeType);
        
        Assert.Null(actual);
    }
    
    [Fact]
    public void GetProcessor_NoSuchChallengeIdentifier_ShouldReturnNull()
    {
        var processorMock = new Mock<IChallengeProcessor>();
        var identifier = new ChallengeIdentifier("user", "id");
        processorMock.Setup(x => x.HasChallengeContext(It.IsAny<ChallengeIdentifier>())).Returns(false);
        var processor = processorMock.Object;
        var provider = new ChallengeProcessorProvider([processor]);
        var actual = provider.GetChallengeProcessorByIdentifier(identifier);
        
        Assert.Null(actual);
    }
}