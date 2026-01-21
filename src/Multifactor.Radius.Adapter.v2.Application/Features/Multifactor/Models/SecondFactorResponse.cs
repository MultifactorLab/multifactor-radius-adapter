using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;

public class SecondFactorResponse {
    public AuthenticationStatus Code { get; }
    public string? ReplyMessage { get; }
    public string? State { get; } = null;
    public SecondFactorResponse(AuthenticationStatus code, string? state = null, string? replyMessage = null)
    {
        Code = code;
        ReplyMessage = replyMessage;
        State = state;
    }
}