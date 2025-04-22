using Moq;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests;

public class ExecutePipelineTests
{
    [Fact]
    public async Task ShouldExecutePipeline()
    {
        var mock1 = new Mock<IRadiusPipelineStep>();
        var mock2 = new Mock<IRadiusPipelineStep>();
        var steps = new IRadiusPipelineStep[]
        {
            mock1.Object,
            mock2.Object
        };

        var pipelineExecutor = new RadiusRadiusPipelineExecutor(steps);
        var context = new Mock<IRadiusPipelineExecutionContext>().Object;
        await pipelineExecutor.ExecuteAsync(context);
        mock1.Verify(s => s.ExecuteAsync(context), Times.Once);
        mock2.Verify(s => s.ExecuteAsync(context), Times.Once);
    }
}