namespace Multifactor.Radius.Adapter.v2.Core.AccessChallenge;

public interface IChallengeProcessorProvider
{
    IChallengeProcessor? GetChallengeProcessorByIdentifier(ChallengeIdentifier identifier);
    IChallengeProcessor? GetChallengeProcessorByType(ChallengeType type);
}