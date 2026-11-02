using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.SecondFactor.Models;

internal sealed record ChallengeRequestQuery(
    string Identity,
    string Challenge,
    string RequestId,
    ChallengeCapabilities Capabilities)
{
    public static ChallengeRequestQuery FromAppModel(ChallengeRequestDto query)
        => new(query.Identity, query.Challenge, query.RequestId, new ChallengeCapabilities(MobilePushOtp: true));
}

internal sealed record ChallengeCapabilities(bool MobilePushOtp);
