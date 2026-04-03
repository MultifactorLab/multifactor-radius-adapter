using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Ports;

public interface ISendChallenge
{
    Task<AccessRequestResponse> Execute(ChallengeRequestDto query, MultifactorAuthData authData, CancellationToken cancellationToken = default);
}