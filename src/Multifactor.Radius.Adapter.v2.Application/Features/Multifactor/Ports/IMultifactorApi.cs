using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Ports;

public interface IMultifactorApi
{
    Task<AccessRequestResponse> CreateAccessRequest(AccessRequestQuery query, MultifactorAuthData authData, CancellationToken cancellationToken = default);
    Task<AccessRequestResponse> SendChallengeAsync(ChallengeRequestQuery query, MultifactorAuthData authData, CancellationToken cancellationToken = default);
}