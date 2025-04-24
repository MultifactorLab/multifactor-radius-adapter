using Moq;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Builder;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests;

public class BuildPipelineTests
{
    [Fact]
    public void NoPipelineSteps_ShouldReturnPipeline()
    {
        var pipelineBuilder = new PipelineBuilder();
        var pipeline = pipelineBuilder.Build();
        Assert.NotNull(pipeline);
    }

    [Fact]
    public void ShouldBuildPipeline()
    {
        var mock1 = new Mock<IRadiusPipelineStep>();
        var mock2 = new Mock<IRadiusPipelineStep>();
        var pipelineBuilder = new PipelineBuilder();
        pipelineBuilder
            .AddPipelineStep(mock1.Object)
            .AddPipelineStep(mock2.Object);
        var pipeline = pipelineBuilder.Build();
        Assert.NotNull(pipeline);
    }
}