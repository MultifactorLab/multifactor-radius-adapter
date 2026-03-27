using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.AccessChallenge.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.AccessChallenge.Models.Enums;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps.AccessChallenge;

public interface IChallengeProcessorProvider
{
    IChallengeProcessor? GetChallengeProcessorByIdentifier(ChallengeIdentifier identifier);
    IChallengeProcessor? GetChallengeProcessorByType(ChallengeType type);
}