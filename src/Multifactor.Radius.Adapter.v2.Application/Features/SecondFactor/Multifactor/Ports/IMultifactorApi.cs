using Multifactor.Radius.Adapter.v2.Application.Features.SecondFactor.Multifactor.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.SecondFactor.Multifactor.Ports;

public interface IMultifactorApi
{
    Task<AccessRequestResponse> CreateAccessRequest(AccessRequestQuery query, MultifactorAuthData authData, CancellationToken cancellationToken = default);
    Task<AccessRequestResponse> SendChallengeAsync(ChallengeRequestDto query, MultifactorAuthData authData, CancellationToken cancellationToken = default);
}