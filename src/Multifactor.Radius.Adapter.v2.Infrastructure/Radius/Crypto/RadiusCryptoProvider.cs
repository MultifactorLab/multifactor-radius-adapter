using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Crypto;

public class RadiusCryptoProvider : IRadiusCryptoProvider
{
    private readonly ILogger<RadiusCryptoProvider> _logger;

    public RadiusCryptoProvider(ILogger<RadiusCryptoProvider> logger)
    {
        _logger = logger;
    }

    public byte[] CalculateRequestAuthenticator(SharedSecret secret, byte[] packet)
    {
        return CalculateAuthenticator(secret, packet, new byte[16]);
    }

    public byte[] CalculateResponseAuthenticator(SharedSecret secret, byte[] requestAuth, byte[] responsePacket)
    {
        return CalculateAuthenticator(secret, responsePacket, requestAuth);
    }

    public byte[] CalculateMessageAuthenticator(SharedSecret secret, byte[] packet, RadiusAuthenticator? requestAuth = null)
    {
        using var hmac = new HMACMD5(secret.Bytes.ToArray());
        
        if (requestAuth != null)
        {
            // For response packets, use request authenticator
            var tempPacket = new byte[packet.Length];
            packet.CopyTo(tempPacket, 0);
            requestAuth.Value.CopyTo(tempPacket, 4);
            return hmac.ComputeHash(tempPacket);
        }
        
        return hmac.ComputeHash(packet);
    }

    public bool ValidateMessageAuthenticator(
        byte[] packet,
        byte[] messageAuth,
        int position,
        SharedSecret secret,
        RadiusAuthenticator? requestAuth = null)
    {
        var tempPacket = new byte[packet.Length];
        packet.CopyTo(tempPacket, 0);
        
        for (int i = 0; i < 16; i++)
        {
            tempPacket[position + 2 + i] = 0;
        }
        var calculated = CalculateMessageAuthenticator(secret, tempPacket, requestAuth);
        
        return CryptographicOperations.FixedTimeEquals(calculated, messageAuth);
    }

    public byte[] EncryptPassword(SharedSecret secret, RadiusAuthenticator authenticator, byte[] password)
    {
        return RadiusPasswordProtector.Encrypt(secret, authenticator, password);
    }

    public byte[] DecryptPassword(SharedSecret secret, RadiusAuthenticator authenticator, byte[] encryptedPassword)
    {
        return RadiusPasswordProtector.Decrypt(secret, authenticator, encryptedPassword);
    }

    private byte[] CalculateAuthenticator(SharedSecret secret, byte[] packet, byte[] requestAuth)
    {
        var buffer = new byte[packet.Length + secret.Bytes.Length];
        packet.CopyTo(buffer, 0);
        secret.Bytes.CopyTo(buffer.AsMemory(packet.Length));
        
        if (requestAuth.Length == 16)
        {
            requestAuth.CopyTo(buffer, 4);
        }

        using var md5 = MD5.Create();
        return md5.ComputeHash(buffer);
    }
}