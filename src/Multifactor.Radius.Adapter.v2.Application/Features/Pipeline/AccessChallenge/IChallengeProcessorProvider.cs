using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.AccessChallenge;

public interface IChallengeProcessorProvider
{
    IChallengeProcessor? GetChallengeProcessorByIdentifier(ChallengeIdentifier identifier);
    IChallengeProcessor? GetChallengeProcessorByType(ChallengeType type);
}