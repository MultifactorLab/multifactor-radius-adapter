using Multifactor.Radius.Adapter.v2.Application.Core;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Interfaces;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;

public class RadiusPipeline : IRadiusPipeline
{
    private readonly List<IRadiusPipelineStep> _steps;
    
    public RadiusPipeline(List<IRadiusPipelineStep> steps)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
    }
    
    public async Task ExecuteAsync(RadiusPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        foreach (var step in _steps)
        {
            await step.ExecuteAsync(context);
            
            if (context.IsTerminated)
            {
                break;
            }
        }
    }
}