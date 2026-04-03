using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases;

internal interface IRadiusPipelineStep
{
    Task ExecuteAsync(RadiusPipelineContext context);
}