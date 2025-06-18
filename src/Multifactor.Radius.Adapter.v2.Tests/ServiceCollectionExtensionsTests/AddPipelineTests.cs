

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Extensions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Services;

namespace Multifactor.Radius.Adapter.v2.Tests.ServiceCollectionExtensionsTests;

public class AddPipelineTests
{
    [Fact]
    public void AddPipelineSteps_ShouldAddPipeline()
    {
        var pipelineKey = "MyPipeline";
        var host = Host.CreateApplicationBuilder();
        host.Services.AddSingleton(new ApplicationVariables());
        var configuration = new PipelineConfiguration([typeof(StatusServerFilteringStep), typeof(AccessRequestFilteringStep)]);
        host.Services.AddPipeline(pipelineKey, configuration);
        var app = host.Build();
        var pipeline = app.Services.GetKeyedService<IRadiusPipeline>(pipelineKey);
        Assert.NotNull(pipeline);
    }
    
    [Fact]
    public void AddPipeline_ShouldAddTwoPipelines()
    {
        var pipelineKey1 = "1";
        var pipelineKey2 = "2";
        
        var host = Host.CreateApplicationBuilder();
        
        var configuration1 = new PipelineConfiguration([typeof(StatusServerFilteringStep), typeof(AccessRequestFilteringStep)]);
        var configuration2 = new PipelineConfiguration([typeof(CheckingMembershipStep), typeof(AccessRequestFilteringStep)]);
        host.Services.AddSingleton(new ApplicationVariables());
        host.Services.AddPipeline(pipelineKey1, configuration1);
        host.Services.AddPipeline(pipelineKey2, configuration2);
        var app = host.Build();
        
        var pipeline1 = app.Services.GetKeyedService<IRadiusPipeline>(pipelineKey1);
        var pipeline2 = app.Services.GetKeyedService<IRadiusPipeline>(pipelineKey2);
        
        Assert.NotNull(pipeline1);
        Assert.NotNull(pipeline2);
    }

    [Fact]
    public void NoPipelineRegistry_ShouldReturnNull()
    {
        var pipelineKey1 = "1";
        var host = Host.CreateApplicationBuilder();
        var app = host.Build();
        var pipeline = app.Services.GetKeyedService<IRadiusPipeline>(pipelineKey1);
        Assert.Null(pipeline);
    }

    [Fact]
    public void StepTypeDoesNotImplementIPipelineInterface_ShouldThrow()
    {
        var pipelineKey = "MyPipeline";
        var host = Host.CreateApplicationBuilder();
        var stepsTypes = new PipelineConfiguration([typeof(XmlAppConfigurationSource)]);
        Assert.Throws<ArgumentException>(() => host.Services.AddPipeline(pipelineKey, stepsTypes));
    }
}