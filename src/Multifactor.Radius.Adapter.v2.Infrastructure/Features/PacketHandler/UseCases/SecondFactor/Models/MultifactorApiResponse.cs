namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.SecondFactor.Models;

internal sealed class MultiFactorApiResponse<T>
{
    public bool Success { get; init; }
    public T? Model { get; init; }
}