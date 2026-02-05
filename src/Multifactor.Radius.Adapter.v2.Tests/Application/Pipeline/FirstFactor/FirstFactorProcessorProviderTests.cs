using Moq;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline.FirstFactor
{
    public class FirstFactorProcessorProviderTests
    {
        [Fact]
        public void Constructor_ShouldThrowWhenProcessorsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FirstFactorProcessorProvider(null));
        }

        [Fact]
        public void GetProcessor_ShouldReturnCorrectProcessor()
        {
            // Arrange
            var radiusProcessor = new Mock<IFirstFactorProcessor>();
            radiusProcessor.Setup(x => x.AuthenticationSource).Returns(AuthenticationSource.Radius);
            
            var ldapProcessor = new Mock<IFirstFactorProcessor>();
            ldapProcessor.Setup(x => x.AuthenticationSource).Returns(AuthenticationSource.Ldap);
            
            var processors = new[] { radiusProcessor.Object, ldapProcessor.Object };
            var provider = new FirstFactorProcessorProvider(processors);

            // Act
            var result = provider.GetProcessor(AuthenticationSource.Ldap);

            // Assert
            Assert.Equal(ldapProcessor.Object, result);
        }

        [Fact]
        public void GetProcessor_ShouldThrowWhenProcessorNotFound()
        {
            // Arrange
            var processor = new Mock<IFirstFactorProcessor>();
            processor.Setup(x => x.AuthenticationSource).Returns(AuthenticationSource.Radius);
            
            var provider = new FirstFactorProcessorProvider(new[] { processor.Object });

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => provider.GetProcessor(AuthenticationSource.Ldap));
            
            Assert.Contains("No processor found", exception.Message);
        }

        [Fact]
        public void GetProcessor_ShouldHandleMultipleProcessorsWithSameSource()
        {
            // Arrange
            var processor1 = new Mock<IFirstFactorProcessor>();
            processor1.Setup(x => x.AuthenticationSource).Returns(AuthenticationSource.Radius);
            
            var processor2 = new Mock<IFirstFactorProcessor>();
            processor2.Setup(x => x.AuthenticationSource).Returns(AuthenticationSource.Radius);
            
            var provider = new FirstFactorProcessorProvider(new[] { processor1.Object, processor2.Object });

            // Act
            var result = provider.GetProcessor(AuthenticationSource.Radius);

            // Assert - должен вернуть первый найденный
            Assert.NotNull(result);
            Assert.Equal(AuthenticationSource.Radius, result.AuthenticationSource);
        }
    }
}