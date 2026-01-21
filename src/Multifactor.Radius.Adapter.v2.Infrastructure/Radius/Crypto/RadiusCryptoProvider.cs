using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

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
        var temp = new byte[packet.Length];
        packet.CopyTo(temp, 0);

        requestAuth?.Value.CopyTo(temp, 4);

        using var md5 = new HMACMD5(secret.Bytes);
        return md5.ComputeHash(temp);
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
        return calculated.SequenceEqual(messageAuth);
        // return CryptographicOperations.FixedTimeEquals(calculated, messageAuth);
    }

    public byte[] DecryptPassword(SharedSecret secret, RadiusAuthenticator authenticator, byte[] encryptedPassword)
    {
        return RadiusPasswordProtector.Decrypt(secret, authenticator, encryptedPassword);
    }

    private static byte[] CalculateAuthenticator(SharedSecret secret, byte[] packet, byte[] requestAuth)
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