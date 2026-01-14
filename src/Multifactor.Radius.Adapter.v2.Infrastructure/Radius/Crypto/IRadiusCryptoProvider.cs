using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Crypto;

public interface IRadiusCryptoProvider
{
    byte[] CalculateRequestAuthenticator(SharedSecret secret, byte[] packet);
    byte[] CalculateResponseAuthenticator(SharedSecret secret, byte[] requestAuth, byte[] responsePacket);
    byte[] CalculateMessageAuthenticator(SharedSecret secret, byte[] packet, RadiusAuthenticator? requestAuth = null);
    bool ValidateMessageAuthenticator(byte[] packet, byte[] messageAuth, int position, SharedSecret secret, RadiusAuthenticator? requestAuth = null);
    byte[] EncryptPassword(SharedSecret secret, RadiusAuthenticator authenticator, byte[] password);
    byte[] DecryptPassword(SharedSecret secret, RadiusAuthenticator authenticator, byte[] encryptedPassword);
}