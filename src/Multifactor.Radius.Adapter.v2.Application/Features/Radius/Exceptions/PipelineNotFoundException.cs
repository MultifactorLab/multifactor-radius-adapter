namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Exceptions;

public class PipelineNotFoundException : Exception
{
    public string ClientName { get; }
    
    public PipelineNotFoundException(string message, string clientName) 
        : base(message)
    {
        ClientName = clientName;
    }
}