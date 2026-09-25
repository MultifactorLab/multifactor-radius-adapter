namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;

public record ChallengeRequestDto(
    string Identity,
    string Challenge,
    string RequestId,
    ChallengeCapabilities Capabilities);

public sealed record ChallengeCapabilities(bool MobilePushOtp);
