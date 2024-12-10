namespace MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;

public interface IChallengeProcessorProvider
{
    IChallengeProcessor GetChallengeProcessorForIdentifier(ChallengeIdentifier identifier);
    IChallengeProcessor GetChallengeProcessorByType(ChallengeType type);
}