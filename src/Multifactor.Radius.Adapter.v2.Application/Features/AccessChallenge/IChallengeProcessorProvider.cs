using Multifactor.Radius.Adapter.v2.Application.Features.AccessChallenge.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.AccessChallenge;

public interface IChallengeProcessorProvider
{
    IChallengeProcessor? GetChallengeProcessorByIdentifier(ChallengeIdentifier identifier);
    IChallengeProcessor? GetChallengeProcessorByType(ChallengeType type);
}