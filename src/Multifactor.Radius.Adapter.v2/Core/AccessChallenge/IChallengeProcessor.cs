using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Core.AccessChallenge;

public interface IChallengeProcessor
{
    ChallengeIdentifier AddChallengeContext(IRadiusPipelineExecutionContext context);
    bool HasChallengeContext(ChallengeIdentifier identifier);
    Task<ChallengeStatus> ProcessChallengeAsync(ChallengeIdentifier identifier, IRadiusPipelineExecutionContext context);
    public ChallengeType ChallengeType { get; }
}