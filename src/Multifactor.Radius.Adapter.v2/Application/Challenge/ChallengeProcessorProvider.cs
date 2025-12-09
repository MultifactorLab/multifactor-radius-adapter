using Multifactor.Radius.Adapter.v2.Application.Challenge.Interface;
using Multifactor.Radius.Adapter.v2.Domain.Challenge;

namespace Multifactor.Radius.Adapter.v2.Application.Challenge;

public class ChallengeProcessorProvider : IChallengeProcessorProvider
{
    private readonly Dictionary<ChallengeType, IChallengeProcessor> _processorsByType;
    private readonly Dictionary<string, IChallengeProcessor> _processorsByIdentifier;

    public ChallengeProcessorProvider(IEnumerable<IChallengeProcessor> challengeProcessors)
    {
        _processorsByType = challengeProcessors.ToDictionary(p => p.ChallengeType);
        _processorsByIdentifier = new Dictionary<string, IChallengeProcessor>();
    }

    public IChallengeProcessor? GetChallengeProcessorByIdentifier(ChallengeIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        
        if (_processorsByIdentifier.TryGetValue(identifier.Value, out var processor))
            return processor;

        var foundProcessor = _processorsByType.Values.FirstOrDefault(x => x.HasChallengeContext(identifier));
        if (foundProcessor != null)
            _processorsByIdentifier[identifier.Value] = foundProcessor;

        return foundProcessor;
    }

    public IChallengeProcessor? GetChallengeProcessorByType(ChallengeType type)
    {
        _processorsByType.TryGetValue(type, out var processor);
        return processor;
    }
}