using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SharedServices.ChallengeProcessor;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SharedServices;

internal interface IChallengeProcessorProvider
{
    IChallengeProcessor? GetChallengeProcessorByIdentifier(ChallengeIdentifier identifier);
    IChallengeProcessor? GetChallengeProcessorByType(ChallengeType type);
}

internal sealed class ChallengeProcessorProvider : IChallengeProcessorProvider
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