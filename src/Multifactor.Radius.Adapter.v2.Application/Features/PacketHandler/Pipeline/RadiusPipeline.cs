using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Pipeline;

public interface IRadiusPipeline
{
    Task ExecuteAsync(RadiusPipelineContext context);
}

internal sealed class RadiusPipeline : IRadiusPipeline
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