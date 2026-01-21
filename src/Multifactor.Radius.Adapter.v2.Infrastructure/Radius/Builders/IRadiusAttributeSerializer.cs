using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Builders;

public interface IRadiusAttributeSerializer
{
    byte[]? Serialize(string attributeName, object value, RadiusAuthenticator authenticator, SharedSecret sharedSecret, RadiusAuthenticator? requestAuthenticator = null);
}
