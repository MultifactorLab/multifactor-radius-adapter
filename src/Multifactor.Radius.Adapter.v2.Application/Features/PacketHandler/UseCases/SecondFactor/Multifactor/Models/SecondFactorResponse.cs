using Multifactor.Radius.Adapter.v2.Application.Core.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;

public sealed class SecondFactorResponse {
    public AuthenticationStatus Code { get; }
    public string? ReplyMessage { get; }
    public string? State { get; }
    public SecondFactorResponse(AuthenticationStatus code, string? state = null, string? replyMessage = null)
    {
        Code = code;
        ReplyMessage = replyMessage;
        State = state;
    }
}