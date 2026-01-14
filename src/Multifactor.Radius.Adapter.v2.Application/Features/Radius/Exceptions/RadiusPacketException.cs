namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Exceptions;

public class RadiusPacketException: Exception
{
    public RadiusPacketException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
