using Microsoft.Extensions.Logging;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Interfaces;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline
{
    public class RadiusPipelineProviderTests
    {
        private readonly Mock<IRadiusPipelineFactory> _pipelineFactoryMock;
        private readonly Mock<ILogger<RadiusPipelineProvider>> _loggerMock;
        private readonly RadiusPipelineProvider _provider;

        public RadiusPipelineProviderTests()
        {
            _pipelineFactoryMock = new Mock<IRadiusPipelineFactory>();
            _loggerMock = new Mock<ILogger<RadiusPipelineProvider>>();
            _provider = new RadiusPipelineProvider(_pipelineFactoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void GetPipeline_ShouldCreateNewPipelineOnFirstCall()
        {
            // Arrange
            var clientConfig = new ClientConfiguration { Name = "Client1" };
            var expectedPipeline = Mock.Of<IRadiusPipeline>();
            
            _pipelineFactoryMock
                .Setup(x => x.CreatePipeline(clientConfig))
                .Returns(expectedPipeline);

            // Act
            var pipeline = _provider.GetPipeline(clientConfig);

            // Assert
            Assert.Equal(expectedPipeline, pipeline);
            _pipelineFactoryMock.Verify(x => x.CreatePipeline(clientConfig), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Creating new pipeline")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void GetPipeline_ShouldReturnCachedPipelineOnSubsequentCalls()
        {
            // Arrange
            var clientConfig = new ClientConfiguration { Name = "Client1" };
            var expectedPipeline = Mock.Of<IRadiusPipeline>();
            
            _pipelineFactoryMock
                .Setup(x => x.CreatePipeline(clientConfig))
                .Returns(expectedPipeline);

            // Act
            var pipeline1 = _provider.GetPipeline(clientConfig);
            var pipeline2 = _provider.GetPipeline(clientConfig);
            var pipeline3 = _provider.GetPipeline(clientConfig);

            // Assert
            Assert.Equal(expectedPipeline, pipeline1);
            Assert.Equal(expectedPipeline, pipeline2);
            Assert.Equal(expectedPipeline, pipeline3);
            _pipelineFactoryMock.Verify(x => x.CreatePipeline(clientConfig), Times.Once);
        }

        [Fact]
        public void GetPipeline_ShouldCacheDifferentClientsSeparately()
        {
            // Arrange
            var client1 = new ClientConfiguration { Name = "Client1" };
            var client2 = new ClientConfiguration { Name = "Client2" };
            
            var pipeline1 = Mock.Of<IRadiusPipeline>();
            var pipeline2 = Mock.Of<IRadiusPipeline>();
            
            _pipelineFactoryMock
                .Setup(x => x.CreatePipeline(client1))
                .Returns(pipeline1);
            
            _pipelineFactoryMock
                .Setup(x => x.CreatePipeline(client2))
                .Returns(pipeline2);

            // Act
            var result1 = _provider.GetPipeline(client1);
            var result2 = _provider.GetPipeline(client2);
            var result1Again = _provider.GetPipeline(client1);

            // Assert
            Assert.Equal(pipeline1, result1);
            Assert.Equal(pipeline2, result2);
            Assert.Equal(pipeline1, result1Again);
            _pipelineFactoryMock.Verify(x => x.CreatePipeline(client1), Times.Once);
            _pipelineFactoryMock.Verify(x => x.CreatePipeline(client2), Times.Once);
        }

        [Fact]
        public void GetPipeline_ShouldThrowWhenClientNameIsNull()
        {
            // Arrange
            var clientConfig = new ClientConfiguration { Name = null };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _provider.GetPipeline(clientConfig));
        }
    }
}