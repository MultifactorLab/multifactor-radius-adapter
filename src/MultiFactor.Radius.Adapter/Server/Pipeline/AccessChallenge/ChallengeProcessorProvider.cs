using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;

public class ChallengeProcessorProvider : IChallengeProcessorProvider
{
    private readonly IEnumerable<IChallengeProcessor> _challengeProcessors;
    
    public ChallengeProcessorProvider(IEnumerable<IChallengeProcessor> challengeProcessors)
    {
        _challengeProcessors = challengeProcessors;
    }

    public IChallengeProcessor GetChallengeProcessorForIdentifier(ChallengeIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        return _challengeProcessors.FirstOrDefault(x => x.HasChallengeContext(identifier));
    }
    
    public IChallengeProcessor GetChallengeProcessorByType(ChallengeType type)
    {
        return _challengeProcessors.FirstOrDefault(x => x.ChallengeType == type);
    }
}