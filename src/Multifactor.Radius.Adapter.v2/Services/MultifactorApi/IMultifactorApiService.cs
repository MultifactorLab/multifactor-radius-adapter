using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;

namespace Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

public interface IMultifactorApiService
{
    Task<MultifactorResponse> CreateSecondFactorRequestAsync(CreateSecondFactorRequest context);
    Task<MultifactorResponse> SendChallengeAsync(SendChallengeRequest request);
}