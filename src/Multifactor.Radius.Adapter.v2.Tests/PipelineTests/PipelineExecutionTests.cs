using Moq;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Builder;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests;

public class PipelineExecutionTests
{
    [Fact]
    public async Task ShouldExecuteEmptyPipeline()
    {
        var pipelineBuilder = new PipelineBuilder();
        
        var pipeline = pipelineBuilder.Build();
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.ExecutionState).Returns(new ExecutionState());
        var context = contextMock.Object;
        await pipeline!.ExecuteAsync(context);
    }
    
    [Fact]
    public async Task ShouldExecutePipelineInRightOrder()
    {
        var executionChain = new List<int>(3);
        var pipelineBuilder = new PipelineBuilder();
        pipelineBuilder
            .AddPipelineStep(new StepMock(1,executionChain))
            .AddPipelineStep(new StepMock(2,executionChain))
            .AddPipelineStep(new StepMock(3,executionChain));
        
        var pipeline = pipelineBuilder.Build();
        
        var contextMock = new Mock<IRadiusPipelineExecutionContext>();
        contextMock.Setup(x => x.ExecutionState).Returns(new ExecutionState());
        var context = contextMock.Object;
        await pipeline!.ExecuteAsync(context);
        
        Assert.Equal(3, executionChain.Count);
        Assert.Collection(executionChain,
            e => Assert.Equal(1, e),
            e => Assert.Equal(2, e),
            e => Assert.Equal(3, e));
    }
    
    private class StepMock : IRadiusPipelineStep
    {
        private readonly int _step;
        private readonly List<int> _stepChain;
        public StepMock(int stepNumber, List<int> stepChain)
        {
            _step = stepNumber;
            _stepChain = stepChain;
        }

        public Task ExecuteAsync(IRadiusPipelineExecutionContext context)
        {
            _stepChain.Add(_step);
            return Task.CompletedTask;
        }
    }
}