using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;
using Microsoft.Extensions.Caching.Memory;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class PipelineStepTypesFactory : IPipelineStepsTypeFactory
{
    private readonly IMemoryCache _memoryCache;

    public PipelineStepTypesFactory(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Type[] GetPipelineStepTypes(IPipelineStepsConfiguration pipelineStepsConfiguration)
    {
        var existedPipeline = GetExistedPipeline(pipelineStepsConfiguration.ConfigurationName);
        if (existedPipeline != null)
        {
            return existedPipeline;
        }

        var newPipeline = BuildNewPipeline(pipelineStepsConfiguration);
        _memoryCache.Set(pipelineStepsConfiguration.ConfigurationName, newPipeline);
        return newPipeline;
    }

    private Type[]? GetExistedPipeline(string pipelineName)
    {
        if (!_memoryCache.TryGetValue(pipelineName, out Type[]? pipeline))
        {
            return null;
        }

        return pipeline;
    }

    private Type[] BuildNewPipeline(IPipelineStepsConfiguration pipelineStepsConfiguration)
    {
        var pipeline = new List<Type>();

        pipeline.Add(typeof(StatusServerFilteringStep));

        pipeline.Add(typeof(AccessRequestFilteringStep));

        pipeline.Add(typeof(ProfileLoadingStep));

        pipeline.Add(typeof(AccessChallengeStep));

        if (pipelineStepsConfiguration.PreAuthMode != PreAuthMode.None)
        {
            pipeline.Add(typeof(SecondFactorStep));
            pipeline.Add(typeof(FirstFactorStep));
            if (pipelineStepsConfiguration.ShouldCheckMembership)
            {
                pipeline.Add(typeof(CheckingMembershipStep));
            }
        }
        else
        {
            pipeline.Add(typeof(FirstFactorStep));
            if (pipelineStepsConfiguration.ShouldCheckMembership)
            {
                pipeline.Add(typeof(CheckingMembershipStep));
            }

            pipeline.Add(typeof(SecondFactorStep));
        }

        pipeline.Add(typeof(RequestPostProcessStep));

        return pipeline.ToArray();
    }
}