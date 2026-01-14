using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Parsers;

public interface IRadiusAttributeParser
{
    ParsedAttribute? Parse(byte[] attributeData, RadiusAuthenticator authenticator, SharedSecret sharedSecret);
}