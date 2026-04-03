using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Ports;

public interface ICreateAccessRequest
{
    Task<AccessRequestResponse> Execute(AccessRequestDto query, MultifactorAuthData authData, CancellationToken cancellationToken = default);
}