using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Ports;

public interface IMultifactorApi
{
    Task<AccessRequestResponse> CreateAccessRequest(AccessRequestQuery query, CancellationToken cancellationToken = default);
    Task<AccessRequestResponse> SendChallengeAsync(ChallengeRequestQuery query, CancellationToken cancellationToken = default);
}