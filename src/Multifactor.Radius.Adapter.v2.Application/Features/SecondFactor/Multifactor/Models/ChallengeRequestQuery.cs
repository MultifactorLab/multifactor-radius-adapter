namespace Multifactor.Radius.Adapter.v2.Application.Features.SecondFactor.Multifactor.Models;

public record ChallengeRequestDto(string Identity, string Challenge, string RequestId);