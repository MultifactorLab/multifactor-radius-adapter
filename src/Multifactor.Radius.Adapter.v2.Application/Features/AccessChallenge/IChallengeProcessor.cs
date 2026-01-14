using Multifactor.Radius.Adapter.v2.Application.Features.AccessChallenge.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.AccessChallenge;

public interface IChallengeProcessor
{
    //TODO DO NOT change context. Must return some response with required data
    ChallengeIdentifier AddChallengeContext(RadiusPipelineContext context);
    bool HasChallengeContext(ChallengeIdentifier identifier);
    Task<ChallengeStatus> ProcessChallengeAsync(ChallengeIdentifier identifier, RadiusPipelineContext context);
    public ChallengeType ChallengeType { get; }
}