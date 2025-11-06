using Multifactor.Radius.Adapter.v2.Core.Auth;

namespace Multifactor.Radius.Adapter.v2.Core.MultifactorApi;

public class MultifactorResponse
{
    public AuthenticationStatus Code { get; }
    
    public string? ReplyMessage { get; }
    public string? State { get; } = null;

    public MultifactorResponse(AuthenticationStatus code, string? state = null, string? replyMessage = null)
    {
        Code = code;
        ReplyMessage = replyMessage;
        State = state;
    }
}