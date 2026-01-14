namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Exceptions;

public class RadiusProcessingException : Exception
{
    public string ClientName { get; }
    
    public RadiusProcessingException(string message) : base(message) { }
    
    public RadiusProcessingException(string message, Exception innerException) 
        : base(message, innerException) { }
    
    public RadiusProcessingException(string message, string clientName) 
        : base(message)
    {
        ClientName = clientName;
    }
    
    public RadiusProcessingException(string message, string clientName, Exception innerException) 
        : base(message, innerException)
    {
        ClientName = clientName;
    }
}