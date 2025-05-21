using System.Security.Cryptography;
using MultiFactor.Radius.Adapter.Core.Radius;

namespace Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Radius;

internal static class RadiusPacketFactory
{
    public static IRadiusPacket? AccessRequest(SharedSecret? packetSecret = null, byte identifier = 0)
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessRequest, identifier);
        var sharedSecret = packetSecret ?? new SharedSecret(Convert.ToHexString(GenerateSecret()).ToLower());
        var packet = new RadiusPacket(header, new RadiusAuthenticator(), sharedSecret);
        return packet;
    }
    
    public static IRadiusPacket? AccessChallenge(SharedSecret? packetSecret = null, byte identifier = 0)
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessChallenge, identifier);
        var sharedSecret = packetSecret ?? new SharedSecret(Convert.ToHexString(GenerateSecret()).ToLower());
        var packet = new RadiusPacket(header, new RadiusAuthenticator(), sharedSecret);
        return packet;
    }
    
    public static IRadiusPacket? AccessReject(SharedSecret? packetSecret = null, byte identifier = 0)
    {
        var header = RadiusPacketHeader.Create(PacketCode.AccessReject, identifier);
        var sharedSecret = packetSecret ?? new SharedSecret(Convert.ToHexString(GenerateSecret()).ToLower());
        var packet = new RadiusPacket(header, new RadiusAuthenticator(), sharedSecret);
        return packet;
    }
    
    public static IRadiusPacket? StatusServer(SharedSecret? packetSecret = null, byte identifier = 0)
    {
        var header = RadiusPacketHeader.Create(PacketCode.StatusServer, identifier);
        var sharedSecret = packetSecret ?? new SharedSecret(Convert.ToHexString(GenerateSecret()).ToLower());
        var packet = new RadiusPacket(header, new RadiusAuthenticator(), sharedSecret);
        return packet;
    }

    private static byte[] GenerateSecret()
    {
        using var rng = RandomNumberGenerator.Create();      
        var data = new byte[16];
        // Fill the salt with cryptographically strong byte values.
        rng.GetNonZeroBytes(data);
        return data; 
    }
}