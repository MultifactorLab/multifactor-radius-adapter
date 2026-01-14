using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;

public class RadiusPipeline : IRadiusPipeline
{
    private readonly List<IRadiusPipelineStep> _steps;
    private readonly ILogger<RadiusPipeline> _logger;
    
    public RadiusPipeline(List<IRadiusPipelineStep> steps)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        // _logger = logger ?? throw new ArgumentNullException(nameof(logger)); TODO fix or return
    }
    
    public async Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("Starting pipeline execution with {StepCount} steps", _steps.Count);

        foreach (var step in _steps)
        {
            await step.ExecuteAsync(context);
            
            if (context.IsTerminated)
            {
                _logger.LogDebug("Pipeline terminated early at step {StepName}", 
                    step.GetType().Name);
                break;
            }
        }
    }
}