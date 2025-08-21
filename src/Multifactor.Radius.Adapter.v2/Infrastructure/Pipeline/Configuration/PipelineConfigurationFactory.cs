using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Multifactor.Radius.Adapter.v2.Core.Auth.PreAuthMode;
using Multifactor.Radius.Adapter.v2.Services.Cache;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class PipelineConfigurationFactory : IPipelineConfigurationFactory
{
    private readonly ICacheService _cache;

    public PipelineConfigurationFactory(ICacheService cache)
    {
        _cache = cache;
    }

    public PipelineConfiguration CreatePipelineConfiguration(IPipelineStepsConfiguration pipelineStepsConfiguration)
    {
        var existedPipeline = GetExistedPipeline(pipelineStepsConfiguration.ConfigurationName);
        if (existedPipeline != null)
        {
            return existedPipeline;
        }

        PipelineConfiguration newPipeline = BuildNewPipeline(pipelineStepsConfiguration);
        _cache.Set(pipelineStepsConfiguration.ConfigurationName, newPipeline);
        return newPipeline;
    }

    private PipelineConfiguration? GetExistedPipeline(string pipelineName)
    {
        if (!_cache.TryGetValue(pipelineName, out PipelineConfiguration? pipeline))
            return null;

        return pipeline;
    }

    private PipelineConfiguration BuildNewPipeline(IPipelineStepsConfiguration pipelineStepsConfiguration)
    {
        return pipelineStepsConfiguration.HasLdapServers ? GetPipelineWithLdap(pipelineStepsConfiguration) : GetPipelineWithoutLdap(pipelineStepsConfiguration);
    }

    private PipelineConfiguration GetPipelineWithoutLdap(IPipelineStepsConfiguration pipelineStepsConfiguration)
    {
        var pipeline = new List<Type>();

        pipeline.Add(typeof(StatusServerFilteringStep));
        pipeline.Add(typeof(AccessRequestFilteringStep));
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

    private PipelineConfiguration GetPipelineWithLdap(IPipelineStepsConfiguration pipelineStepsConfiguration)
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

        if (pipelineStepsConfiguration.ShouldLoadUserGroups)
            pipeline.Add(typeof(UserGroupLoadingStep));

        return new PipelineConfiguration(pipeline.ToArray());
    }
}