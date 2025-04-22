using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddPipelineSteps(this IServiceCollection services, string pipelineKey, Type[] pipelineStepTypes)
    {
        foreach (var stepType in pipelineStepTypes)
        {
            if (!typeof(IRadiusPipelineStep).IsAssignableFrom(stepType))
            {
                throw new ArgumentException(
                    $"The type {stepType.FullName} does not implement {nameof(IRadiusPipelineStep)}");
            }
            
            services.TryAddSingleton(stepType);
        }

        services.AddKeyedSingleton<IRadiusPipelineStep[]>(pipelineKey, (serviceProvider, x) =>
        {
            var pipelineSteps = new List<IRadiusPipelineStep>();
            foreach (var type in pipelineStepTypes)
            {
                var step = serviceProvider.GetRequiredService(type) as IRadiusPipelineStep;
                if (step is null)
                    throw new InvalidOperationException($"Type {type} does not implement IPipelineStep");
                pipelineSteps.Add(step);
            }

            return pipelineSteps.ToArray();
        });
    }
}