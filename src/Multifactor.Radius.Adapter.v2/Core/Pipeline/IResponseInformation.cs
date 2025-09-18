using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

namespace Multifactor.Radius.Adapter.v2.Core.Pipeline;

public interface IResponseInformation
{
    string? ReplyMessage { get; set; }
    
    string? State { get; set; }

    Dictionary<string, RadiusAttribute> Attributes { get; set; }
}