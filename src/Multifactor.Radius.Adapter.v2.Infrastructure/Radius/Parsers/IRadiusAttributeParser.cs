using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Parsers;

public interface IRadiusAttributeParser
{
    public ParsedAttribute? Parse(byte[] attributeData, byte typeCode, RadiusAuthenticator authenticator,
        SharedSecret sharedSecret);
}