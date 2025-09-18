using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.Core.Pipeline;

public class ResponseInformation : IResponseInformation
{
    public string? ReplyMessage { get; set; }
    
    public string? State { get; set; }
    
    public Dictionary<string, RadiusAttribute> Attributes { get; set; } = new();
}