using Moq;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models.Enums;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.AccessChallenge
{
    public class ChallengeProcessorProviderTests
    {
        [Fact]
        public void GetChallengeProcessorByIdentifier_ShouldReturnProcessorWithContext()
        {
            // Arrange
            var identifier = new ChallengeIdentifier("client1", "request123");
            var processor1 = new Mock<IChallengeProcessor>();
            processor1.Setup(x => x.HasChallengeContext(identifier)).Returns(false);
            var processor2 = new Mock<IChallengeProcessor>();
            processor2.Setup(x => x.HasChallengeContext(identifier)).Returns(true);
            var provider = new ChallengeProcessorProvider(new[] { processor1.Object, processor2.Object });

            // Act
            var result = provider.GetChallengeProcessorByIdentifier(identifier);

            // Assert
            Assert.Equal(processor2.Object, result);
        }

        [Fact]
        public void GetChallengeProcessorByIdentifier_ShouldReturnNullWhenNoProcessorHasContext()
        {
            // Arrange
            var identifier = new ChallengeIdentifier("client1", "request123");
            
            var processor = new Mock<IChallengeProcessor>();
            processor.Setup(x => x.HasChallengeContext(identifier)).Returns(false);
            var provider = new ChallengeProcessorProvider([processor.Object]);

            // Act
            var result = provider.GetChallengeProcessorByIdentifier(identifier);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetChallengeProcessorByIdentifier_ShouldThrowWhenIdentifierNull()
        {
            // Arrange
            var provider = new ChallengeProcessorProvider([]);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => provider.GetChallengeProcessorByIdentifier(null));
        }

        [Fact]
        public void GetChallengeProcessorByType_ShouldReturnCorrectProcessor()
        {
            // Arrange
            var processor1 = new Mock<IChallengeProcessor>();
            processor1.Setup(x => x.ChallengeType).Returns(ChallengeType.SecondFactor);
            
            var processor2 = new Mock<IChallengeProcessor>();
            processor2.Setup(x => x.ChallengeType).Returns(ChallengeType.PasswordChange);
            
            var provider = new ChallengeProcessorProvider([processor1.Object, processor2.Object]);

            // Act
            var result = provider.GetChallengeProcessorByType(ChallengeType.PasswordChange);

            // Assert
            Assert.Equal(processor2.Object, result);
        }

        [Fact]
        public void GetChallengeProcessorByType_ShouldReturnNullWhenTypeNotFound()
        {
            // Arrange
            var processor = new Mock<IChallengeProcessor>();
            processor.Setup(x => x.ChallengeType).Returns(ChallengeType.SecondFactor);
            
            var provider = new ChallengeProcessorProvider(new[] { processor.Object });

            // Act
            var result = provider.GetChallengeProcessorByType(ChallengeType.PasswordChange);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetChallengeProcessorByType_ShouldReturnNullWhenNoProcessors()
        {
            // Arrange
            var provider = new ChallengeProcessorProvider(Array.Empty<IChallengeProcessor>());

            // Act
            var result = provider.GetChallengeProcessorByType(ChallengeType.SecondFactor);

            // Assert
            Assert.Null(result);
        }
    }
}