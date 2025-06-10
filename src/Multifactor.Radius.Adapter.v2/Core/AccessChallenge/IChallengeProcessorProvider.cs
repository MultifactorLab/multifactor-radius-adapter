namespace Multifactor.Radius.Adapter.v2.Core.AccessChallenge;

public interface IChallengeProcessorProvider
{
    IChallengeProcessor? GetChallengeProcessorForIdentifier(ChallengeIdentifier identifier);
    IChallengeProcessor? GetChallengeProcessorByType(ChallengeType type);
}