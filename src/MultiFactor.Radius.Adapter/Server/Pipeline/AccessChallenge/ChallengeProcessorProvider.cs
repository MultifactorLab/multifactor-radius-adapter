using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Server.Pipeline.AccessChallenge;

public class ChallengeProcessorProvider : IChallengeProcessorProvider
{
    private readonly IEnumerable<IChallengeProcessor> _challengeProcessors;
    
    public ChallengeProcessorProvider(IEnumerable<IChallengeProcessor> challengeProcessors)
    {
        _challengeProcessors = challengeProcessors ?? throw new ArgumentNullException(nameof(challengeProcessors));
    }

    public IChallengeProcessor GetChallengeProcessorForIdentifier(ChallengeIdentifier identifier)
    {
        if (identifier == null)
        {
            throw new ArgumentNullException(nameof(identifier));
        }

        return _challengeProcessors.FirstOrDefault(x => x.HasChallengeContext(identifier));
    }
    
    public IChallengeProcessor GetChallengeProcessorByType(ChallengeType type)
    {
        return _challengeProcessors.FirstOrDefault(x => x.ChallengeType == type);
    }
}