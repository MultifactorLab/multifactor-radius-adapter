namespace Multifactor.Radius.Adapter.v2.Core.Pipeline;

public interface IResponseInformation
{
    string? ReplyMessage { get; set; }
    
    public string? State { get; set; }
}