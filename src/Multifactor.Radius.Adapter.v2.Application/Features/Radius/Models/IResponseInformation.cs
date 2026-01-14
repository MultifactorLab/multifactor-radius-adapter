namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

public interface IResponseInformation
{
    string? ReplyMessage { get; set; }
    
    public string? State { get; set; }
}