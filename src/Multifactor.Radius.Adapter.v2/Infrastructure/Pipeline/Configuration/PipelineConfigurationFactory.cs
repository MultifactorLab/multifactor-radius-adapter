using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Microsoft.Extensions.Caching.Memory;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class PipelineConfigurationFactory : IPipelineConfigurationFactory
{
    private readonly IMemoryCache _memoryCache;

    public PipelineConfigurationFactory(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public PipelineConfiguration CreatePipelineConfiguration(IPipelineStepsConfiguration pipelineStepsConfiguration)
    {
        var existedPipeline = GetExistedPipeline(pipelineStepsConfiguration.ConfigurationName);
        if (existedPipeline != null)
        {
            return existedPipeline;
        }

        PipelineConfiguration newPipeline = BuildNewPipeline(pipelineStepsConfiguration);
        _memoryCache.Set(pipelineStepsConfiguration.ConfigurationName, newPipeline);
        return newPipeline;
    }

    private PipelineConfiguration? GetExistedPipeline(string pipelineName)
    {
        if (!_memoryCache.TryGetValue(pipelineName, out PipelineConfiguration? pipeline))
        {
            return null;
        }

        return pipeline;
    }

    private PipelineConfiguration BuildNewPipeline(IPipelineStepsConfiguration pipelineStepsConfiguration)
    {
        var pipeline = new List<Type>();

        pipeline.Add(typeof(StatusServerFilteringStep));

        pipeline.Add(typeof(AccessRequestFilteringStep));
        
        pipeline.Add(typeof(LdapSchemaLoadingStep));

        pipeline.Add(typeof(ProfileLoadingStep));
        
        pipeline.Add(typeof(AccessGroupsCheckingStep));

        pipeline.Add(typeof(AccessChallengeStep));

        if (pipelineStepsConfiguration.PreAuthMode != PreAuthMode.None)
        {
            pipeline.Add(typeof(PreAuthCheckStep));
            pipeline.Add(typeof(SecondFactorStep));
            pipeline.Add(typeof(PreAuthPostCheck));
            pipeline.Add(typeof(FirstFactorStep));
        }
        else
        {
            pipeline.Add(typeof(FirstFactorStep));
            pipeline.Add(typeof(SecondFactorStep));
        }

        return new PipelineConfiguration(pipeline.ToArray());
    }
}