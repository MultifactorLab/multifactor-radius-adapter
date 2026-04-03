using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SharedServices.ChallengeProcessor;

internal interface IChallengeProcessor
{
    //TODO DO NOT change context. Must return some response with required data
    ChallengeIdentifier AddChallengeContext(RadiusPipelineContext context);
    bool HasChallengeContext(ChallengeIdentifier identifier);
    Task<ChallengeStatus> ProcessChallengeAsync(ChallengeIdentifier identifier, RadiusPipelineContext context);
    public ChallengeType ChallengeType { get; }
}