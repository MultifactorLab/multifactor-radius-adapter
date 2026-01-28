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
        
        // Replace the Message-Authenticator content only.
        // messageAuthenticatorPosition is a position of the Message-Authenticator block.
        // The full-block length is 18: typecode (1), length (1), content (16).
        // So the Message-Authenticator content position is (messageAuthenticatorPosition + 2).
        Buffer.BlockCopy(new byte[16], 0, tempPacket, position + 2, 16);

        var calculatedMessageAuthenticator =
            CalculateMessageAuthenticator(secret, tempPacket, requestAuth);
        return calculatedMessageAuthenticator.SequenceEqual(messageAuth);
    }

    public byte[] DecryptPassword(SharedSecret secret, RadiusAuthenticator authenticator, byte[] encryptedPassword)
    {
        return RadiusPasswordProtector.Decrypt(secret, authenticator, encryptedPassword);
    }

    private static byte[] CalculateAuthenticator(SharedSecret secret, byte[] packet, byte[] requestAuth)
    {
        var responseAuthenticator = packet.Concat(secret.Bytes).ToArray();
        Buffer.BlockCopy(requestAuth, 0, responseAuthenticator, 4, 16);

        using var md5 = MD5.Create();
        return md5.ComputeHash(responseAuthenticator);
    }
}