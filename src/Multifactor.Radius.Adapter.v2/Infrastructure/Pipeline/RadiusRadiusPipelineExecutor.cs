using Microsoft.Extensions.DependencyInjection;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class RadiusRadiusPipelineExecutor : IRadiusPipelineExecutor
{
    private readonly IRadiusPipelineStep[] _steps;
    
    public RadiusRadiusPipelineExecutor(IRadiusPipelineStep[] steps)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }

    public async Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        if(context == null) throw new ArgumentNullException(nameof(context));

        foreach (var step in _steps)
        {
            await step.ExecuteAsync(context);
        }
    }
}