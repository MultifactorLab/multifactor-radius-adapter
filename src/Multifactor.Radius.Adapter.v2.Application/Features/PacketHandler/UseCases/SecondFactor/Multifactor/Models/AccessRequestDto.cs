namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;

public sealed record AccessRequestDto(
    string? Identity,
    string? Name,
    string? Email,
    string? Phone,
    string? PassCode,
    string? CallingStationId,
    string? CalledStationId,
    string? SignUpGroups);