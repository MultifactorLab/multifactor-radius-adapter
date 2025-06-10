using Multifactor.Radius.Adapter.v2.Core.AccessChallenge;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

public interface IMultifactorApiService
{
    Task<MultifactorResponse> CreateSecondFactorRequestAsync(IRadiusPipelineExecutionContext context);
    Task<MultifactorResponse> SendChallengeAsync(IRadiusPipelineExecutionContext context, string answer, ChallengeIdentifier identifier);
}