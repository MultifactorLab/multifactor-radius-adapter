using System.Diagnostics;
using Moq;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Builder;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Xunit.Abstractions;

namespace Multifactor.Radius.Adapter.v2.Tests.PipelineTests;


public class PerformanceTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public PerformanceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void PipelineTest(int stepsCount)
    {
        var builder = new PipelineBuilder();
        for (int i = 0; i < stepsCount; i++)
        {
            builder.AddPipelineStep(new StepMock());
        }

        var pipeline = builder.Build();
        var sw = Stopwatch.StartNew();
        pipeline.ExecuteAsync(new Mock<IRadiusPipelineExecutionContext>().Object);
        sw.Stop();
        _testOutputHelper.WriteLine(sw.Elapsed.ToString());
    }
    
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void ForTest(int stepsCount)
    {
        var steps = new List<IRadiusPipelineStep>(stepsCount);
        for (int i = 0; i < stepsCount; i++)
        {
            steps.Add(new StepMock());
        }
        var sw = Stopwatch.StartNew();
        foreach (var step in steps)
        {
            step.ExecuteAsync(new Mock<IRadiusPipelineExecutionContext>().Object);
        }
        
        sw.Stop();
        _testOutputHelper.WriteLine(sw.Elapsed.ToString());
    }
    
    private class StepMock : IRadiusPipelineStep
    {
        public Task ExecuteAsync(IRadiusPipelineExecutionContext context)
        {
            return Task.CompletedTask;
        }
    }
}