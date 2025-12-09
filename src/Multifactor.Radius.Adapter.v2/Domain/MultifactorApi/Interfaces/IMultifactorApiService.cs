using Multifactor.Radius.Adapter.v2.Infrastructure.MultifactorApi.Dto;

namespace Multifactor.Radius.Adapter.v2.Domain.MultifactorApi.Interfaces;

public interface IMultifactorApiService
{
    Task<MultifactorResponse> CreateSecondFactorRequestAsync(CreateSecondFactorRequest context);
    Task<MultifactorResponse> SendChallengeAsync(SendChallengeRequest request);
}