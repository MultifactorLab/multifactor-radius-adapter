using System.Net;
using Moq;
using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Pipeline
{
    public class RadiusPipelineTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldExecuteAllStepsInOrder()
        {
            // Arrange
            var executionOrder = new List<string>();
            var steps = CreateMockSteps(3, executionOrder);
            var pipeline = new RadiusPipeline(steps);
            var context = new RadiusPipelineContext(
                CreateRequestPacket(),
                new ClientConfiguration());

            // Act
            await pipeline.ExecuteAsync(context);

            // Assert
            Assert.Equal(new[] { "Step1", "Step2", "Step3" }, executionOrder);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldStopExecutionWhenTerminated()
        {
            // Arrange
            var executionOrder = new List<string>();
            var steps = new List<IRadiusPipelineStep>
            {
                CreateMockStep("Step1", executionOrder, terminate: false),
                CreateMockStep("Step2", executionOrder, terminate: true),
                CreateMockStep("Step3", executionOrder, terminate: false) // Этот шаг не должен выполниться
            };
            
            var pipeline = new RadiusPipeline(steps);
            var context = new RadiusPipelineContext(
                CreateRequestPacket(),
                new ClientConfiguration());

            // Act
            await pipeline.ExecuteAsync(context);

            // Assert
            Assert.Equal(new[] { "Step1", "Step2" }, executionOrder);
            Assert.True(context.IsTerminated);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleStepExceptions()
        {
            // Arrange
            var step1 = new Mock<IRadiusPipelineStep>();
            var step2 = new Mock<IRadiusPipelineStep>();
            
            step1.Setup(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()))
                .ThrowsAsync(new InvalidOperationException("Step1 failed"));
            
            step2.Setup(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()))
                .Returns(Task.CompletedTask);
            
            var steps = new List<IRadiusPipelineStep> { step1.Object, step2.Object };
            var pipeline = new RadiusPipeline(steps);
            var context = new RadiusPipelineContext(
                CreateRequestPacket(),
                new ClientConfiguration());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => pipeline.ExecuteAsync(context));
            
            // Step2 не должен был выполниться после исключения
            step2.Verify(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()), Times.Never);
        }

        [Fact]
        public void Constructor_ShouldThrowWhenStepsIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RadiusPipeline(null));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldThrowWhenContextIsNull()
        {
            // Arrange
            var pipeline = new RadiusPipeline(new List<IRadiusPipelineStep>());

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => pipeline.ExecuteAsync(null));
        }

        private List<IRadiusPipelineStep> CreateMockSteps(int count, List<string> executionOrder)
        {
            var steps = new List<IRadiusPipelineStep>();
            
            for (int i = 1; i <= count; i++)
            {
                steps.Add(CreateMockStep($"Step{i}", executionOrder, terminate: false));
            }
            
            return steps;
        }

        private IRadiusPipelineStep CreateMockStep(string name, List<string> executionOrder, bool terminate)
        {
            var mock = new Mock<IRadiusPipelineStep>();
            mock.Setup(x => x.ExecuteAsync(It.IsAny<RadiusPipelineContext>()))
                .Callback<RadiusPipelineContext>(ctx =>
                {
                    executionOrder.Add(name);
                    if (terminate)
                    {
                        ctx.Terminate();
                    }
                })
                .Returns(Task.CompletedTask);
            
            return mock.Object;
        }
        
        private RadiusPacket CreateRequestPacket()
        {
            var header = new RadiusPacketHeader(PacketCode.AccessRequest, 1, new byte[16]);
            var packet = new RadiusPacket(header)
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, 1812)
            };
            packet.AddAttributeValue("User-Name", "testuser");
            return packet;
        }
    }
}