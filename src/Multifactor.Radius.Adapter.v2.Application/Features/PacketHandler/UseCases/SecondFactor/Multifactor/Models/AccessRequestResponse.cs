using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;

public sealed class AccessRequestResponse
{
    public string Id { get; init; } = string.Empty;
    public string Identity { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public RequestStatus Status { get; init; }
    public string? ReplyMessage { get; init; } = string.Empty;
    public bool Bypassed { get; init; }
    public string Authenticator { get; init; } = string.Empty;
    public string AuthenticatorId { get; init; } = string.Empty;
    public string Account { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string Region { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;

    public static AccessRequestResponse Bypass => new() { Status = RequestStatus.Granted, Bypassed = true };
}