using Multifactor.Radius.Adapter.v2.Domain.Challenge;

namespace Multifactor.Radius.Adapter.v2.Application.Challenge.Interface;

public interface IChallengeProcessorProvider
{
    IChallengeProcessor? GetChallengeProcessorByIdentifier(ChallengeIdentifier identifier);
    IChallengeProcessor? GetChallengeProcessorByType(ChallengeType type);
}