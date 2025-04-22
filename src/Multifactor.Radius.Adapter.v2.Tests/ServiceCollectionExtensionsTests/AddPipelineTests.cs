

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Multifactor.Radius.Adapter.v2.Core.Extensions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Tests.ServiceCollectionExtensionsTests;

public class AddPipelineTests
{
    [Fact]
    public void AddPipelineSteps_ShouldAddPipelineSteps()
    {
        var pipelineKey = "MyPipeline";
        var host = Host.CreateApplicationBuilder();
        var stepsTypes = new Type[] { typeof(StatusServerFilteringStep), typeof(AccessRequestFilteringStep) };
        host.Services.AddPipelineSteps(pipelineKey, stepsTypes);
        var app = host.Build();
        var pipeline = app.Services.GetKeyedService<IRadiusPipelineStep[]>(pipelineKey);
        Assert.NotNull(pipeline);
        Assert.Equal(2, pipeline.Length);
        Assert.Collection(pipeline,
            e => IsType<StatusServerFilteringStep>(),
            e => IsType<AccessRequestFilteringStep>());
    }
    
    [Fact]
    public void AddPipelineSteps_ShouldAddTwoPipelines()
    {
        var pipelineKey1 = "1";
        var pipelineKey2 = "2";
        
        var host = Host.CreateApplicationBuilder();
        
        var stepsTypes1 = new Type[] { typeof(StatusServerFilteringStep), typeof(AccessRequestFilteringStep) };
        var stepsTypes2 = new Type[] { typeof(CheckingMembershipStep), typeof(RequestPostProcessStep), typeof(AccessRequestFilteringStep) };
        
        host.Services.AddPipelineSteps(pipelineKey1, stepsTypes1);
        host.Services.AddPipelineSteps(pipelineKey2, stepsTypes2);
        var app = host.Build();
        
        var pipeline1 = app.Services.GetKeyedService<IRadiusPipelineStep[]>(pipelineKey1);
        var pipeline2 = app.Services.GetKeyedService<IRadiusPipelineStep[]>(pipelineKey2);
        
        Assert.NotNull(pipeline1);
        Assert.Equal(2, pipeline1.Length);
        Assert.Collection(
            pipeline1,
            IsType<StatusServerFilteringStep>(),
            IsType<AccessRequestFilteringStep>());
        
        Assert.NotNull(pipeline2);
        Assert.Equal(3, pipeline2.Length);
        Assert.Collection(
            pipeline2,
            IsType<CheckingMembershipStep>(),
            IsType<RequestPostProcessStep>(),
            IsType<AccessRequestFilteringStep>());
    }

    [Fact]
    public void NoPipelineSteps_ShouldReturnNull()
    {
        var pipelineKey1 = "1";
        var host = Host.CreateApplicationBuilder();
        var app = host.Build();
        var pipeline = app.Services.GetKeyedService<IRadiusPipelineStep[]>(pipelineKey1);
        Assert.Null(pipeline);
    }

    [Fact]
    public void StepTypeDoesNotImplementIPipelineInterface_ShouldThrow()
    {
        var pipelineKey = "MyPipeline";
        var host = Host.CreateApplicationBuilder();
        var stepsTypes = new Type[] { typeof(XmlAppConfigurationSource) };
        Assert.Throws<ArgumentException>(() => host.Services.AddPipelineSteps(pipelineKey, stepsTypes));
    }

    private Action<IRadiusPipelineStep> IsType<T>()
    {
        return (IRadiusPipelineStep e) => Assert.IsType<T>(e);
    }
}