namespace Multifactor.Radius.Adapter.v2.Core.AccessChallenge;

public class ChallengeProcessorProvider : IChallengeProcessorProvider
{
    private readonly IEnumerable<IChallengeProcessor> _challengeProcessors;
    
    public ChallengeProcessorProvider(IEnumerable<IChallengeProcessor> challengeProcessors)
    {
        _challengeProcessors = challengeProcessors;
    }
    
    public IChallengeProcessor? GetChallengeProcessorByIdentifier(ChallengeIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        return _challengeProcessors.FirstOrDefault(x => x.HasChallengeContext(identifier));
    }

    public IChallengeProcessor? GetChallengeProcessorByType(ChallengeType type)
    {
        return _challengeProcessors.FirstOrDefault(x => x.ChallengeType == type);
    }
}