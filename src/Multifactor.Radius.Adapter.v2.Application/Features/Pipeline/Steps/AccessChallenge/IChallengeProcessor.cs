using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.AccessChallenge.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.AccessChallenge.Models.Enums;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.AccessChallenge;

public interface IChallengeProcessor
{
    //TODO DO NOT change context. Must return some response with required data
    ChallengeIdentifier AddChallengeContext(RadiusPipelineContext context);
    bool HasChallengeContext(ChallengeIdentifier identifier);
    Task<ChallengeStatus> ProcessChallengeAsync(ChallengeIdentifier identifier, RadiusPipelineContext context);
    public ChallengeType ChallengeType { get; }
}